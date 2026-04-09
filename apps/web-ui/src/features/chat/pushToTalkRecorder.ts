export interface PushToTalkStopResult {
  fullBlob: Blob | null;
  trailingBlob: Blob | null;
  durationMs: number;
}

export interface PushToTalkRecorderOptions {
  chunkIntervalMs?: number;
  onChunk?: (chunk: Blob) => Promise<void> | void;
  onDuration?: (durationMs: number) => void;
}

export interface PushToTalkRecorder {
  start: () => Promise<void>;
  stop: () => Promise<PushToTalkStopResult>;
  cancel: () => Promise<void>;
  dispose: () => Promise<void>;
  isRecording: () => boolean;
}

const DEFAULT_CHUNK_INTERVAL_MS = 1200;

type BrowserAudioContext = typeof AudioContext;

function getAudioContextConstructor(): BrowserAudioContext | null {
  if (typeof window === "undefined") {
    return null;
  }

  const browserWindow = window as Window & {
    webkitAudioContext?: BrowserAudioContext;
  };

  if (typeof AudioContext !== "undefined") {
    return AudioContext;
  }

  return browserWindow.webkitAudioContext ?? null;
}

function mergeFloat32Chunks(chunks: Float32Array[]): Float32Array {
  const totalLength = chunks.reduce((sum, chunk) => sum + chunk.length, 0);
  const merged = new Float32Array(totalLength);
  let offset = 0;
  for (const chunk of chunks) {
    merged.set(chunk, offset);
    offset += chunk.length;
  }
  return merged;
}

function clampSample(sample: number): number {
  if (sample > 1) {
    return 1;
  }
  if (sample < -1) {
    return -1;
  }
  return sample;
}

function writeAscii(view: DataView, offset: number, value: string): void {
  for (let index = 0; index < value.length; index += 1) {
    view.setUint8(offset + index, value.charCodeAt(index));
  }
}

function encodeWav(samples: Float32Array, sampleRate: number): Blob {
  const buffer = new ArrayBuffer(44 + samples.length * 2);
  const view = new DataView(buffer);

  writeAscii(view, 0, "RIFF");
  view.setUint32(4, 36 + samples.length * 2, true);
  writeAscii(view, 8, "WAVE");
  writeAscii(view, 12, "fmt ");
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, 1, true);
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * 2, true);
  view.setUint16(32, 2, true);
  view.setUint16(34, 16, true);
  writeAscii(view, 36, "data");
  view.setUint32(40, samples.length * 2, true);

  let offset = 44;
  for (const sample of samples) {
    const clamped = clampSample(sample);
    view.setInt16(offset, clamped < 0 ? clamped * 0x8000 : clamped * 0x7fff, true);
    offset += 2;
  }

  return new Blob([buffer], { type: "audio/wav" });
}

function resetChunks(target: Float32Array[][]): Float32Array[] {
  const chunks = target[0];
  target[0] = [];
  return chunks;
}

export function createPushToTalkRecorder(
  options: PushToTalkRecorderOptions = {},
): PushToTalkRecorder {
  let stream: MediaStream | null = null;
  let context: AudioContext | null = null;
  let source: MediaStreamAudioSourceNode | null = null;
  let processor: ScriptProcessorNode | null = null;
  let chunkIntervalId: number | null = null;
  let recording = false;
  let sampleRate = 44100;
  let totalSamples = 0;
  let pendingChunkDispatch: Promise<void> = Promise.resolve();

  const allChunksRef: Float32Array[][] = [[]];
  const trailingChunksRef: Float32Array[][] = [[]];

  const clearIntervalIfNeeded = () => {
    if (chunkIntervalId !== null) {
      window.clearInterval(chunkIntervalId);
      chunkIntervalId = null;
    }
  };

  const teardownGraph = async () => {
    processor?.disconnect();
    source?.disconnect();
    stream?.getTracks().forEach((track) => track.stop());

    processor = null;
    source = null;
    stream = null;

    if (context && context.state !== "closed") {
      await context.close();
    }
    context = null;
  };

  const queuePartialChunk = () => {
    if (!recording || trailingChunksRef[0].length === 0) {
      return;
    }

    const chunk = encodeWav(mergeFloat32Chunks(resetChunks(trailingChunksRef)), sampleRate);
    pendingChunkDispatch = pendingChunkDispatch
      .then(async () => {
        await options.onChunk?.(chunk);
      })
      .catch(() => undefined);
  };

  return {
    async start() {
      if (recording) {
        return;
      }

      const AudioContextCtor = getAudioContextConstructor();
      if (!AudioContextCtor || typeof navigator === "undefined" || !navigator.mediaDevices?.getUserMedia) {
        throw new Error("Microphone capture is not available in this runtime.");
      }

      stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          channelCount: 1,
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
        },
      });

      context = new AudioContextCtor();
      if (context.state === "suspended") {
        await context.resume();
      }

      sampleRate = context.sampleRate;
      totalSamples = 0;
      allChunksRef[0] = [];
      trailingChunksRef[0] = [];

      source = context.createMediaStreamSource(stream);
      processor = context.createScriptProcessor(4096, 1, 1);
      processor.onaudioprocess = (event) => {
        if (!recording) {
          return;
        }

        const input = event.inputBuffer.getChannelData(0);
        const copy = new Float32Array(input.length);
        copy.set(input);
        allChunksRef[0].push(copy);
        trailingChunksRef[0].push(copy);
        totalSamples += copy.length;
        options.onDuration?.(Math.round((totalSamples / sampleRate) * 1000));
      };

      source.connect(processor);
      processor.connect(context.destination);

      recording = true;
      chunkIntervalId = window.setInterval(
        queuePartialChunk,
        options.chunkIntervalMs ?? DEFAULT_CHUNK_INTERVAL_MS,
      );
    },

    async stop() {
      if (!recording) {
        return { fullBlob: null, trailingBlob: null, durationMs: 0 };
      }

      recording = false;
      clearIntervalIfNeeded();
      await pendingChunkDispatch;

      const fullBlob =
        allChunksRef[0].length > 0
          ? encodeWav(mergeFloat32Chunks(resetChunks(allChunksRef)), sampleRate)
          : null;
      const trailingBlob =
        trailingChunksRef[0].length > 0
          ? encodeWav(mergeFloat32Chunks(resetChunks(trailingChunksRef)), sampleRate)
          : null;
      const durationMs = Math.round((totalSamples / sampleRate) * 1000);
      totalSamples = 0;
      options.onDuration?.(0);
      await teardownGraph();

      return {
        fullBlob,
        trailingBlob,
        durationMs,
      };
    },

    async cancel() {
      if (!recording && !context && !stream) {
        return;
      }

      recording = false;
      clearIntervalIfNeeded();
      await pendingChunkDispatch;
      allChunksRef[0] = [];
      trailingChunksRef[0] = [];
      totalSamples = 0;
      options.onDuration?.(0);
      await teardownGraph();
    },

    async dispose() {
      await this.cancel();
    },

    isRecording() {
      return recording;
    },
  };
}
