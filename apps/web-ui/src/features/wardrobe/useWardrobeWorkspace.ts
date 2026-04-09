import { startTransition, useEffect, useState } from "react";
import type {
  WardrobeImportPayload,
  WardrobeItemCreatePayload,
  WardrobeItemRecord,
  WardrobeOutfitCreatePayload,
  WardrobeOutfitRecord,
  WardrobeSnapshotResponse,
} from "@/contracts/backend";
import {
  createWardrobeItem,
  createWardrobeOutfit,
  deleteWardrobeItem,
  deleteWardrobeOutfit,
  exportWardrobeSnapshot,
  getWardrobeSnapshot,
  importWardrobeSnapshot,
  updateWardrobeItem,
  updateWardrobeOutfit,
} from "@/services/backendClient";

export interface WardrobeItemDraft {
  itemId: string;
  displayName: string;
  slot: string;
  source: string;
  sourceAssetPath: string;
  prefabAssetPath: string;
  materialAssetPathsText: string;
  thumbnailAssetPath: string;
  occupiesSlotsText: string;
  blocksSlotsText: string;
  requiresSlotsText: string;
  compatibleTagsText: string;
  incompatibleTagsText: string;
  hideBodyRegionsText: string;
  anchorType: string;
  anchorBoneName: string;
}

export interface WardrobeOutfitDraft {
  outfitId: string;
  displayName: string;
  source: string;
  thumbnailAssetPath: string;
  slotAssignments: Record<string, string>;
}

interface WardrobeWorkspaceState {
  snapshot: WardrobeSnapshotResponse | null;
  loading: boolean;
  mutating: boolean;
  error: string;
  mutationMessage: string;
  exportText: string;
}

export interface WardrobeWorkspace {
  state: WardrobeWorkspaceState;
  itemDraft: WardrobeItemDraft;
  outfitDraft: WardrobeOutfitDraft;
  importText: string;
  itemMode: "create" | "edit";
  outfitMode: "create" | "edit";
  selectedItemId: string | null;
  selectedOutfitId: string | null;
  setItemDraft: (updater: (current: WardrobeItemDraft) => WardrobeItemDraft) => void;
  setOutfitDraft: (updater: (current: WardrobeOutfitDraft) => WardrobeOutfitDraft) => void;
  setImportText: (value: string) => void;
  startCreateItem: () => void;
  startEditItem: (item: WardrobeItemRecord) => void;
  submitItem: () => Promise<void>;
  deleteCurrentItem: () => Promise<void>;
  startCreateOutfit: () => void;
  startEditOutfit: (outfit: WardrobeOutfitRecord) => void;
  setOutfitAssignment: (slot: string, itemId: string) => void;
  submitOutfit: () => Promise<void>;
  deleteCurrentOutfit: () => Promise<void>;
  refresh: () => Promise<void>;
  refreshExport: () => Promise<void>;
  applyImport: (mode: "merge" | "replace") => Promise<void>;
}

function emptyItemDraft(): WardrobeItemDraft {
  return {
    itemId: "",
    displayName: "",
    slot: "hair",
    source: "user",
    sourceAssetPath: "",
    prefabAssetPath: "",
    materialAssetPathsText: "",
    thumbnailAssetPath: "",
    occupiesSlotsText: "",
    blocksSlotsText: "",
    requiresSlotsText: "",
    compatibleTagsText: "",
    incompatibleTagsText: "",
    hideBodyRegionsText: "",
    anchorType: "None",
    anchorBoneName: "",
  };
}

function emptyOutfitDraft(): WardrobeOutfitDraft {
  return {
    outfitId: "",
    displayName: "",
    source: "user",
    thumbnailAssetPath: "",
    slotAssignments: {},
  };
}

function parseList(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter((item, index, values) => item.length > 0 && values.indexOf(item) === index);
}

function itemToDraft(item: WardrobeItemRecord): WardrobeItemDraft {
  return {
    itemId: item.item_id,
    displayName: item.display_name,
    slot: item.slot,
    source: item.source,
    sourceAssetPath: item.source_asset_path ?? "",
    prefabAssetPath: item.prefab_asset_path ?? "",
    materialAssetPathsText: item.material_asset_paths.join(", "),
    thumbnailAssetPath: item.thumbnail_asset_path ?? "",
    occupiesSlotsText: item.occupies_slots.join(", "),
    blocksSlotsText: item.blocks_slots.join(", "),
    requiresSlotsText: item.requires_slots.join(", "),
    compatibleTagsText: item.compatible_tags.join(", "),
    incompatibleTagsText: item.incompatible_tags.join(", "),
    hideBodyRegionsText: item.hide_body_regions.join(", "),
    anchorType: item.anchor_type,
    anchorBoneName: item.anchor_bone_name ?? "",
  };
}

function draftToItemPayload(draft: WardrobeItemDraft): WardrobeItemCreatePayload {
  return {
    item_id: draft.itemId.trim(),
    display_name: draft.displayName.trim(),
    slot: draft.slot.trim(),
    source: draft.source.trim() || "user",
    source_asset_path: draft.sourceAssetPath.trim() || null,
    prefab_asset_path: draft.prefabAssetPath.trim() || null,
    material_asset_paths: parseList(draft.materialAssetPathsText),
    thumbnail_asset_path: draft.thumbnailAssetPath.trim() || null,
    occupies_slots: parseList(draft.occupiesSlotsText),
    blocks_slots: parseList(draft.blocksSlotsText),
    requires_slots: parseList(draft.requiresSlotsText),
    compatible_tags: parseList(draft.compatibleTagsText),
    incompatible_tags: parseList(draft.incompatibleTagsText),
    hide_body_regions: parseList(draft.hideBodyRegionsText),
    anchor_type: draft.anchorType.trim() || "None",
    anchor_bone_name: draft.anchorBoneName.trim() || null,
  };
}

function outfitToDraft(outfit: WardrobeOutfitRecord): WardrobeOutfitDraft {
  return {
    outfitId: outfit.outfit_id,
    displayName: outfit.display_name,
    source: outfit.source,
    thumbnailAssetPath: outfit.thumbnail_asset_path ?? "",
    slotAssignments: { ...outfit.slot_assignments },
  };
}

function draftToOutfitPayload(draft: WardrobeOutfitDraft): WardrobeOutfitCreatePayload {
  const slotAssignments = Object.fromEntries(
    Object.entries(draft.slotAssignments).filter(([, itemId]) => itemId.trim().length > 0),
  );
  return {
    outfit_id: draft.outfitId.trim(),
    display_name: draft.displayName.trim(),
    source: draft.source.trim() || "user",
    thumbnail_asset_path: draft.thumbnailAssetPath.trim() || null,
    slot_assignments: slotAssignments,
  };
}

function prettyJson(payload: WardrobeSnapshotResponse): string {
  return JSON.stringify(payload, null, 2);
}

export function useWardrobeWorkspace(): WardrobeWorkspace {
  const [state, setState] = useState<WardrobeWorkspaceState>({
    snapshot: null,
    loading: true,
    mutating: false,
    error: "",
    mutationMessage: "",
    exportText: "",
  });
  const [itemDraft, setItemDraftState] = useState<WardrobeItemDraft>(() => emptyItemDraft());
  const [outfitDraft, setOutfitDraftState] = useState<WardrobeOutfitDraft>(() => emptyOutfitDraft());
  const [importText, setImportTextState] = useState("");
  const [itemMode, setItemMode] = useState<"create" | "edit">("create");
  const [outfitMode, setOutfitMode] = useState<"create" | "edit">("create");
  const [selectedItemId, setSelectedItemId] = useState<string | null>(null);
  const [selectedOutfitId, setSelectedOutfitId] = useState<string | null>(null);

  const setItemDraft = (updater: (current: WardrobeItemDraft) => WardrobeItemDraft) => {
    startTransition(() => {
      setItemDraftState((current) => updater(current));
    });
  };

  const setOutfitDraft = (updater: (current: WardrobeOutfitDraft) => WardrobeOutfitDraft) => {
    startTransition(() => {
      setOutfitDraftState((current) => updater(current));
    });
  };

  const setImportText = (value: string) => {
    startTransition(() => {
      setImportTextState(value);
    });
  };

  const refresh = async () => {
    try {
      const snapshot = await getWardrobeSnapshot();
      const nextItemId =
        selectedItemId && snapshot.items.some((item) => item.item_id === selectedItemId)
          ? selectedItemId
          : snapshot.items[0]?.item_id ?? null;
      const nextOutfitId =
        selectedOutfitId && snapshot.outfits.some((outfit) => outfit.outfit_id === selectedOutfitId)
          ? selectedOutfitId
          : snapshot.outfits[0]?.outfit_id ?? null;
      const nextItem = snapshot.items.find((item) => item.item_id === nextItemId) ?? null;
      const nextOutfit =
        snapshot.outfits.find((outfit) => outfit.outfit_id === nextOutfitId) ?? null;

      startTransition(() => {
        setState((current) => ({
          ...current,
          snapshot,
          loading: false,
          error: "",
          exportText: prettyJson(snapshot),
        }));
        setSelectedItemId(nextItemId);
        setSelectedOutfitId(nextOutfitId);

        if (nextItem && itemMode === "edit") {
          setItemDraftState(itemToDraft(nextItem));
        } else if (!nextItem && itemMode === "edit") {
          setItemMode("create");
          setItemDraftState(emptyItemDraft());
        } else if (!selectedItemId && nextItem) {
          setItemMode("edit");
          setItemDraftState(itemToDraft(nextItem));
        }

        if (nextOutfit && outfitMode === "edit") {
          setOutfitDraftState(outfitToDraft(nextOutfit));
        } else if (!nextOutfit && outfitMode === "edit") {
          setOutfitMode("create");
          setOutfitDraftState(emptyOutfitDraft());
        } else if (!selectedOutfitId && nextOutfit) {
          setOutfitMode("edit");
          setOutfitDraftState(outfitToDraft(nextOutfit));
        }
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          loading: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const refreshExport = async () => {
    try {
      const snapshot = await exportWardrobeSnapshot();
      startTransition(() => {
        setState((current) => ({
          ...current,
          exportText: prettyJson(snapshot),
          error: "",
        }));
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  const startCreateItem = () => {
    startTransition(() => {
      setItemMode("create");
      setSelectedItemId(null);
      setItemDraftState(emptyItemDraft());
      setState((current) => ({
        ...current,
        mutationMessage: "",
        error: "",
      }));
    });
  };

  const startEditItem = (item: WardrobeItemRecord) => {
    startTransition(() => {
      setItemMode("edit");
      setSelectedItemId(item.item_id);
      setItemDraftState(itemToDraft(item));
      setState((current) => ({
        ...current,
        mutationMessage: "",
        error: "",
      }));
    });
  };

  const submitItem = async () => {
    const payload = draftToItemPayload(itemDraft);
    if (!payload.item_id || !payload.display_name || !payload.slot) {
      setState((current) => ({
        ...current,
        error: "Item id, display name, and slot are required.",
      }));
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const saved =
        itemMode === "edit" && selectedItemId
          ? await updateWardrobeItem(selectedItemId, payload)
          : await createWardrobeItem(payload);

      await refresh();

      startTransition(() => {
        setItemMode("edit");
        setSelectedItemId(saved.item_id);
        setItemDraftState(itemToDraft(saved));
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage:
            itemMode === "edit"
              ? `Updated wardrobe item ${saved.display_name}.`
              : `Created wardrobe item ${saved.display_name}.`,
        }));
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  const deleteCurrentItem = async () => {
    if (!selectedItemId) {
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const snapshot = await deleteWardrobeItem(selectedItemId);
      const nextItem = snapshot.items[0] ?? null;
      const nextOutfit =
        (selectedOutfitId
          ? snapshot.outfits.find((outfit) => outfit.outfit_id === selectedOutfitId)
          : null) ??
        snapshot.outfits[0] ??
        null;
      startTransition(() => {
        setState((current) => ({
          ...current,
          snapshot,
          exportText: prettyJson(snapshot),
          mutating: false,
          mutationMessage: `Removed wardrobe item ${selectedItemId}.`,
        }));
        setSelectedItemId(nextItem?.item_id ?? null);
        if (nextItem) {
          setItemMode("edit");
          setItemDraftState(itemToDraft(nextItem));
        } else {
          setItemMode("create");
          setItemDraftState(emptyItemDraft());
        }
        setSelectedOutfitId(nextOutfit?.outfit_id ?? null);
        if (nextOutfit) {
          setOutfitMode("edit");
          setOutfitDraftState(outfitToDraft(nextOutfit));
        } else {
          setOutfitMode("create");
          setOutfitDraftState(emptyOutfitDraft());
        }
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  const startCreateOutfit = () => {
    startTransition(() => {
      setOutfitMode("create");
      setSelectedOutfitId(null);
      setOutfitDraftState(emptyOutfitDraft());
      setState((current) => ({
        ...current,
        mutationMessage: "",
        error: "",
      }));
    });
  };

  const startEditOutfit = (outfit: WardrobeOutfitRecord) => {
    startTransition(() => {
      setOutfitMode("edit");
      setSelectedOutfitId(outfit.outfit_id);
      setOutfitDraftState(outfitToDraft(outfit));
      setState((current) => ({
        ...current,
        mutationMessage: "",
        error: "",
      }));
    });
  };

  const setOutfitAssignment = (slot: string, itemId: string) => {
    startTransition(() => {
      setOutfitDraftState((current) => ({
        ...current,
        slotAssignments: {
          ...current.slotAssignments,
          [slot]: itemId,
        },
      }));
    });
  };

  const submitOutfit = async () => {
    const payload = draftToOutfitPayload(outfitDraft);
    if (!payload.outfit_id || !payload.display_name) {
      setState((current) => ({
        ...current,
        error: "Outfit id and display name are required.",
      }));
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const saved =
        outfitMode === "edit" && selectedOutfitId
          ? await updateWardrobeOutfit(selectedOutfitId, payload)
          : await createWardrobeOutfit(payload);

      await refresh();

      startTransition(() => {
        setOutfitMode("edit");
        setSelectedOutfitId(saved.outfit_id);
        setOutfitDraftState(outfitToDraft(saved));
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage:
            outfitMode === "edit"
              ? `Updated outfit preset ${saved.display_name}.`
              : `Created outfit preset ${saved.display_name}.`,
        }));
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  const deleteCurrentOutfit = async () => {
    if (!selectedOutfitId) {
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const snapshot = await deleteWardrobeOutfit(selectedOutfitId);
      const nextOutfit = snapshot.outfits[0] ?? null;
      const nextItem =
        (selectedItemId
          ? snapshot.items.find((item) => item.item_id === selectedItemId)
          : null) ??
        snapshot.items[0] ??
        null;
      startTransition(() => {
        setState((current) => ({
          ...current,
          snapshot,
          exportText: prettyJson(snapshot),
          mutating: false,
          mutationMessage: `Removed outfit preset ${selectedOutfitId}.`,
        }));
        setSelectedOutfitId(nextOutfit?.outfit_id ?? null);
        if (nextOutfit) {
          setOutfitMode("edit");
          setOutfitDraftState(outfitToDraft(nextOutfit));
        } else {
          setOutfitMode("create");
          setOutfitDraftState(emptyOutfitDraft());
        }
        setSelectedItemId(nextItem?.item_id ?? null);
        if (nextItem) {
          setItemMode("edit");
          setItemDraftState(itemToDraft(nextItem));
        } else {
          setItemMode("create");
          setItemDraftState(emptyItemDraft());
        }
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  const applyImport = async (mode: "merge" | "replace") => {
    let parsedPayload: WardrobeImportPayload;
    try {
      parsedPayload = JSON.parse(importText) as WardrobeImportPayload;
    } catch {
      setState((current) => ({
        ...current,
        error: "Import JSON is invalid.",
      }));
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const snapshot = await importWardrobeSnapshot({
        mode,
        items: parsedPayload.items ?? [],
        outfits: parsedPayload.outfits ?? [],
      });

      startTransition(() => {
        setState((current) => ({
          ...current,
          snapshot,
          exportText: prettyJson(snapshot),
          mutating: false,
          mutationMessage:
            mode === "replace"
              ? "Replaced wardrobe registry from imported snapshot."
              : "Merged imported wardrobe snapshot into the registry.",
        }));
        setImportTextState("");
      });

      await refresh();
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  return {
    state,
    itemDraft,
    outfitDraft,
    importText,
    itemMode,
    outfitMode,
    selectedItemId,
    selectedOutfitId,
    setItemDraft,
    setOutfitDraft,
    setImportText,
    startCreateItem,
    startEditItem,
    submitItem,
    deleteCurrentItem,
    startCreateOutfit,
    startEditOutfit,
    setOutfitAssignment,
    submitOutfit,
    deleteCurrentOutfit,
    refresh,
    refreshExport,
    applyImport,
  };
}
