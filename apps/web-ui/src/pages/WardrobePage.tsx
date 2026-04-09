import { useDeferredValue, useMemo, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type {
  WardrobeItemRecord,
  WardrobeOutfitRecord,
  WardrobeSlotRecord,
  WardrobeValidationIssue,
} from "@/contracts/backend";
import { useWardrobeWorkspace } from "@/features/wardrobe/useWardrobeWorkspace";
import styles from "./WardrobePage.module.css";

function formatTimestamp(value?: string | null): string {
  if (!value) return "No timestamp";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(parsed);
}

function toneForStatus(status: string): "success" | "sun" | "danger" {
  if (status === "ready") return "success";
  if (status === "blocked") return "danger";
  return "sun";
}

function toneForSeverity(severity: string): "sun" | "danger" {
  return severity === "error" ? "danger" : "sun";
}

function matchesSlot(slot: WardrobeSlotRecord, query: string): boolean {
  if (!query) return true;
  return [slot.key, slot.unity_enum, slot.display_name, slot.notes.join(" ")]
    .join(" ")
    .toLowerCase()
    .includes(query);
}

function matchesItem(item: WardrobeItemRecord, query: string): boolean {
  if (!query) return true;
  return [
    item.item_id,
    item.display_name,
    item.slot,
    item.source,
    item.compatible_tags.join(" "),
    item.incompatible_tags.join(" "),
  ]
    .join(" ")
    .toLowerCase()
    .includes(query);
}

function matchesOutfit(outfit: WardrobeOutfitRecord, query: string): boolean {
  if (!query) return true;
  return [
    outfit.outfit_id,
    outfit.display_name,
    outfit.source,
    Object.keys(outfit.slot_assignments).join(" "),
    Object.values(outfit.slot_assignments).join(" "),
  ]
    .join(" ")
    .toLowerCase()
    .includes(query);
}

function IssuesPanel({
  title,
  status,
  issues,
}: {
  title: string;
  status?: string;
  issues: WardrobeValidationIssue[];
}) {
  return (
    <article className="surface">
      <div className="surfaceHeader">
        <div className="surfaceHeaderBlock">
          <span className="eyebrow">Compatibility</span>
          <h3 className="surfaceTitle">{title}</h3>
        </div>
        {status && (
          <span className="chip" data-tone={toneForStatus(status)}>
            {status}
          </span>
        )}
      </div>
      {issues.length ? (
        <div className={styles.issueList}>
          {issues.map((issue) => (
            <div key={`${issue.code}-${issue.message}`} className={styles.issueCard}>
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <p className="listTitle">{issue.message}</p>
                  <p className="listSubtitle">{issue.code}</p>
                </div>
                <span className="chip" data-tone={toneForSeverity(issue.severity)}>
                  {issue.severity}
                </span>
              </div>
              <div className="chipRow">
                {issue.slots.map((slot) => (
                  <span key={`${issue.code}-${slot}`} className="chip" data-tone="accent">
                    {slot}
                  </span>
                ))}
                {issue.item_ids.map((itemId) => (
                  <span key={`${issue.code}-${itemId}`} className="chip" data-tone="warm">
                    {itemId}
                  </span>
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="emptyState">
          <p className="emptyStateTitle">No active issues</p>
          <p className="emptyStateText">This selection is currently clean.</p>
        </div>
      )}
    </article>
  );
}

export function WardrobePage() {
  const workspace = useWardrobeWorkspace();
  const [searchValue, setSearchValue] = useState("");
  const deferredSearch = useDeferredValue(searchValue.trim().toLowerCase());

  const snapshot = workspace.state.snapshot;
  const slots = snapshot?.slots ?? [];
  const items = snapshot?.items ?? [];
  const outfits = snapshot?.outfits ?? [];

  const filteredSlots = useMemo(() => slots.filter((slot) => matchesSlot(slot, deferredSearch)), [deferredSearch, slots]);
  const filteredItems = useMemo(() => items.filter((item) => matchesItem(item, deferredSearch)), [deferredSearch, items]);
  const filteredOutfits = useMemo(() => outfits.filter((outfit) => matchesOutfit(outfit, deferredSearch)), [deferredSearch, outfits]);
  const selectedItem = items.find((item) => item.item_id === workspace.selectedItemId) ?? null;
  const selectedOutfit = outfits.find((outfit) => outfit.outfit_id === workspace.selectedOutfitId) ?? null;
  const itemsBySlot = useMemo(
    () => slots.reduce<Record<string, WardrobeItemRecord[]>>((map, slot) => {
      map[slot.key] = items.filter((item) => item.slot === slot.key);
      return map;
    }, {}),
    [items, slots],
  );

  return (
    <PageTemplate
      title="Wardrobe"
      icon="WD"
      eyebrow="Registry lane"
      description="A13 lands a backend-backed wardrobe data system with canonical slot taxonomy, JSON registry persistence, preset authoring, compatibility checks, and import or export without claiming live Unity preview."
      highlights={[
        { label: "Items", value: String(snapshot?.summary.item_count ?? 0), detail: `${snapshot?.summary.ready_item_count ?? 0} ready` },
        { label: "Presets", value: String(snapshot?.summary.outfit_count ?? 0), detail: `${snapshot?.summary.ready_outfit_count ?? 0} ready` },
        { label: "Issues", value: String((snapshot?.summary.warning_count ?? 0) + (snapshot?.summary.error_count ?? 0)), detail: `${snapshot?.summary.error_count ?? 0} blocking` },
        { label: "Slots", value: String(snapshot?.summary.slot_count ?? 0), detail: `${snapshot?.summary.reserved_slot_count ?? 0} reserved` },
      ]}
      actions={
        <>
          <button type="button" className="secondaryButton" onClick={() => workspace.startCreateItem()} disabled={workspace.state.mutating}>New item</button>
          <button type="button" className="ghostButton" onClick={() => workspace.startCreateOutfit()} disabled={workspace.state.mutating}>New preset</button>
          <button type="button" className="primaryButton" onClick={() => void workspace.refresh()} disabled={workspace.state.loading || workspace.state.mutating}>Refresh</button>
        </>
      }
    >
      <div className="appStack">
        <article className={`${styles.hero} surface surfaceHero`}>
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Current implementation</span>
              <h3 className="surfaceTitle">Desktop-first wardrobe registry</h3>
              <p className="surfaceIntro">Taxonomy stays contract-driven, data lives in JSON, and preset or compatibility work is now fully desktop-side.</p>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone="accent">taxonomy v{snapshot?.slot_taxonomy_version ?? 1}</span>
              <span className="chip" data-tone="success">registry {snapshot?.version ?? 1}</span>
            </div>
          </div>
          <div className={styles.heroGrid}>
            <div className={styles.heroBlock}><span className="formLabel">Registry path</span><p className="helperText monoText">{snapshot?.registry_path ?? "Loading..."}</p></div>
            <div className={styles.heroBlock}><span className="formLabel">Updated</span><p className="helperText">{formatTimestamp(snapshot?.updated_at)}</p></div>
          </div>
        </article>

        <article className="surface">
          <label className={styles.searchField}>
            <span className="formLabel">Search wardrobe data</span>
            <input className="textInput" type="search" value={searchValue} onChange={(event) => setSearchValue(event.target.value)} placeholder="Search slots, item ids, tags, or preset assignments" />
          </label>
        </article>

        {workspace.state.loading && <article className="surface"><span className="eyebrow">Loading</span><p className="helperText">Refreshing taxonomy, registry items, presets, and export snapshot.</p></article>}
        {!!workspace.state.error && <article className="surface"><span className="eyebrow">Wardrobe issue</span><p className="errorText">{workspace.state.error}</p></article>}
        {workspace.state.mutationMessage && <article className="surface surfaceMuted"><span className="eyebrow">Recent action</span><p className="helperText">{workspace.state.mutationMessage}</p></article>}

        <div className={styles.workspaceGrid}>
          <div className={styles.column}>
            <article className="surface">
              <div className="surfaceHeader"><div className="surfaceHeaderBlock"><span className="eyebrow">Slot taxonomy</span><h3 className="surfaceTitle">Canonical slots</h3></div><span className="chip" data-tone="accent">{filteredSlots.length}</span></div>
              {filteredSlots.length ? (
                <div className={styles.cardList}>
                  {filteredSlots.map((slot) => (
                    <div key={slot.key} className={`${styles.slotCard} ${slot.reserved ? styles.slotCardReserved : ""}`}>
                      <div className="surfaceHeader"><div className="surfaceHeaderBlock"><p className="listTitle">{slot.display_name}</p><p className="listSubtitle">{slot.key} | {slot.unity_enum}</p></div><span className="chip" data-tone={slot.reserved ? "sun" : "accent"}>{slot.reserved ? "reserved" : "active"}</span></div>
                      <div className="chipRow"><span className="chip" data-tone="warm">items {slot.item_count}</span><span className="chip" data-tone="success">presets {slot.outfit_count}</span></div>
                      {slot.notes.length > 0 && <p className="helperText">{slot.notes.join(" ")}</p>}
                    </div>
                  ))}
                </div>
              ) : <div className="emptyState"><p className="emptyStateTitle">No slot match</p><p className="emptyStateText">Clear the search to inspect the full taxonomy.</p></div>}
            </article>

            <IssuesPanel title={selectedItem?.display_name ?? "No item selected"} status={selectedItem?.sync_status} issues={selectedItem?.validation_issues ?? []} />
            <IssuesPanel title={selectedOutfit?.display_name ?? "No preset selected"} status={selectedOutfit?.sync_status} issues={selectedOutfit?.validation_issues ?? []} />
          </div>

          <div className={styles.column}>
            <article className="surface">
              <div className="surfaceHeader"><div className="surfaceHeaderBlock"><span className="eyebrow">Item registry</span><h3 className="surfaceTitle">Wardrobe items</h3></div><span className="chip" data-tone="accent">{filteredItems.length}</span></div>
              {filteredItems.length ? (
                <div className={styles.cardList}>
                  {filteredItems.map((item) => (
                    <button key={item.item_id} type="button" className={`${styles.recordCard} ${workspace.selectedItemId === item.item_id ? styles.recordCardActive : ""}`} onClick={() => workspace.startEditItem(item)}>
                      <div className="surfaceHeader"><div className="surfaceHeaderBlock"><p className="listTitle">{item.display_name}</p><p className="listSubtitle">{item.item_id} | {item.slot}</p></div><span className="chip" data-tone={toneForStatus(item.sync_status)}>{item.sync_status}</span></div>
                      <div className="chipRow"><span className="chip" data-tone="accent">issues {item.validation_issues.length}</span>{item.compatible_tags.slice(0, 2).map((tag) => <span key={`${item.item_id}-${tag}`} className="chip" data-tone="warm">{tag}</span>)}</div>
                      <p className="helperText">Updated {formatTimestamp(item.updated_at)}</p>
                    </button>
                  ))}
                </div>
              ) : <div className="emptyState"><p className="emptyStateTitle">No item match</p><p className="emptyStateText">Create a registry item or adjust the search.</p></div>}
            </article>

            <article className="surface">
              <div className="surfaceHeader"><div className="surfaceHeaderBlock"><span className="eyebrow">Item editor</span><h3 className="surfaceTitle">{workspace.itemMode === "edit" ? "Edit registry item" : "Create registry item"}</h3></div><span className="chip" data-tone={workspace.itemMode === "edit" ? "warm" : "accent"}>{workspace.itemMode}</span></div>
              <div className={styles.editorForm}>
                <div className={styles.fieldGrid}>
                  <label className={styles.fieldBlock}><span className="formLabel">Item id</span><input className="textInput" value={workspace.itemDraft.itemId} disabled={workspace.itemMode === "edit"} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, itemId: event.target.value }))} placeholder="Dress_School_01" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Display name</span><input className="textInput" value={workspace.itemDraft.displayName} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, displayName: event.target.value }))} placeholder="School Dress" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Slot</span><select className="textInput" value={workspace.itemDraft.slot} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, slot: event.target.value }))}>{slots.map((slot) => <option key={slot.key} value={slot.key}>{slot.display_name}</option>)}</select></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Prefab path</span><input className="textInput" value={workspace.itemDraft.prefabAssetPath} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, prefabAssetPath: event.target.value }))} placeholder="ai-dev-system/.../item.fbx" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Occupies slots</span><input className="textInput" value={workspace.itemDraft.occupiesSlotsText} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, occupiesSlotsText: event.target.value }))} placeholder="top, bottom" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Blocks slots</span><input className="textInput" value={workspace.itemDraft.blocksSlotsText} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, blocksSlotsText: event.target.value }))} placeholder="dress" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Requires slots</span><input className="textInput" value={workspace.itemDraft.requiresSlotsText} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, requiresSlotsText: event.target.value }))} placeholder="hair_accessory" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Compatible tags</span><input className="textInput" value={workspace.itemDraft.compatibleTagsText} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, compatibleTagsText: event.target.value }))} placeholder="formal, school" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Incompatible tags</span><input className="textInput" value={workspace.itemDraft.incompatibleTagsText} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, incompatibleTagsText: event.target.value }))} placeholder="oversized" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Hide body regions</span><input className="textInput" value={workspace.itemDraft.hideBodyRegionsText} onChange={(event) => workspace.setItemDraft((current) => ({ ...current, hideBodyRegionsText: event.target.value }))} placeholder="TorsoUpper, TorsoLower" /></label>
                </div>
                <div className="actionRow"><div className="chipRow"><span className="chip" data-tone="accent">slot {workspace.itemDraft.slot}</span></div><div className="chipRow"><button type="button" className="ghostButton" onClick={() => workspace.startCreateItem()} disabled={workspace.state.mutating}>Clear</button>{workspace.itemMode === "edit" && workspace.selectedItemId && <button type="button" className="ghostButton" onClick={() => void workspace.deleteCurrentItem()} disabled={workspace.state.mutating}>Delete item</button>}<button type="button" className="primaryButton" onClick={() => void workspace.submitItem()} disabled={workspace.state.mutating}>{workspace.state.mutating ? "Saving..." : workspace.itemMode === "edit" ? "Save item" : "Create item"}</button></div></div>
              </div>
            </article>
          </div>

          <div className={styles.column}>
            <article className="surface">
              <div className="surfaceHeader"><div className="surfaceHeaderBlock"><span className="eyebrow">Preset library</span><h3 className="surfaceTitle">Outfit presets</h3></div><span className="chip" data-tone="accent">{filteredOutfits.length}</span></div>
              {filteredOutfits.length ? (
                <div className={styles.cardList}>
                  {filteredOutfits.map((outfit) => (
                    <button key={outfit.outfit_id} type="button" className={`${styles.recordCard} ${workspace.selectedOutfitId === outfit.outfit_id ? styles.recordCardActive : ""}`} onClick={() => workspace.startEditOutfit(outfit)}>
                      <div className="surfaceHeader"><div className="surfaceHeaderBlock"><p className="listTitle">{outfit.display_name}</p><p className="listSubtitle">{outfit.outfit_id}</p></div><span className="chip" data-tone={toneForStatus(outfit.sync_status)}>{outfit.sync_status}</span></div>
                      <div className="chipRow"><span className="chip" data-tone="sun">issues {outfit.validation_issues.length}</span>{Object.entries(outfit.slot_assignments).slice(0, 2).map(([slot, itemId]) => <span key={`${outfit.outfit_id}-${slot}`} className="chip" data-tone="accent">{slot}: {itemId}</span>)}</div>
                      <p className="helperText">Updated {formatTimestamp(outfit.updated_at)}</p>
                    </button>
                  ))}
                </div>
              ) : <div className="emptyState"><p className="emptyStateTitle">No preset match</p><p className="emptyStateText">Create a preset or relax the search.</p></div>}
            </article>

            <article className="surface">
              <div className="surfaceHeader"><div className="surfaceHeaderBlock"><span className="eyebrow">Preset editor</span><h3 className="surfaceTitle">{workspace.outfitMode === "edit" ? "Edit preset" : "Create preset"}</h3></div><span className="chip" data-tone={workspace.outfitMode === "edit" ? "warm" : "accent"}>{workspace.outfitMode}</span></div>
              <div className={styles.editorForm}>
                <div className={styles.fieldGrid}>
                  <label className={styles.fieldBlock}><span className="formLabel">Preset id</span><input className="textInput" value={workspace.outfitDraft.outfitId} disabled={workspace.outfitMode === "edit"} onChange={(event) => workspace.setOutfitDraft((current) => ({ ...current, outfitId: event.target.value }))} placeholder="outfit_school_uniform" /></label>
                  <label className={styles.fieldBlock}><span className="formLabel">Display name</span><input className="textInput" value={workspace.outfitDraft.displayName} onChange={(event) => workspace.setOutfitDraft((current) => ({ ...current, displayName: event.target.value }))} placeholder="School Uniform" /></label>
                </div>
                <div className={styles.assignmentGrid}>
                  {slots.map((slot) => (
                    <label key={slot.key} className={styles.fieldBlock}>
                      <span className="formLabel">{slot.display_name}</span>
                      <select className="textInput" value={workspace.outfitDraft.slotAssignments[slot.key] ?? ""} onChange={(event) => workspace.setOutfitAssignment(slot.key, event.target.value)}>
                        <option value="">No item</option>
                        {(itemsBySlot[slot.key] ?? []).map((item) => <option key={item.item_id} value={item.item_id}>{item.display_name}</option>)}
                      </select>
                    </label>
                  ))}
                </div>
                <div className="actionRow"><div className="chipRow"><span className="chip" data-tone="accent">assigned {Object.values(workspace.outfitDraft.slotAssignments).filter(Boolean).length}</span></div><div className="chipRow"><button type="button" className="ghostButton" onClick={() => workspace.startCreateOutfit()} disabled={workspace.state.mutating}>Clear</button>{workspace.outfitMode === "edit" && workspace.selectedOutfitId && <button type="button" className="ghostButton" onClick={() => void workspace.deleteCurrentOutfit()} disabled={workspace.state.mutating}>Delete preset</button>}<button type="button" className="primaryButton" onClick={() => void workspace.submitOutfit()} disabled={workspace.state.mutating}>{workspace.state.mutating ? "Saving..." : workspace.outfitMode === "edit" ? "Save preset" : "Create preset"}</button></div></div>
              </div>
            </article>

            <article className="surface surfaceMuted">
              <div className="surfaceHeader"><div className="surfaceHeaderBlock"><span className="eyebrow">Import or export</span><h3 className="surfaceTitle">Registry JSON</h3></div><button type="button" className="ghostButton" onClick={() => void workspace.refreshExport()} disabled={workspace.state.loading || workspace.state.mutating}>Refresh export</button></div>
              <div className={styles.exportStack}>
                <label className={styles.fieldBlock}><span className="formLabel">Export JSON</span><textarea className={`${styles.codeArea} textArea`} readOnly value={workspace.state.exportText} /></label>
                <label className={styles.fieldBlock}><span className="formLabel">Import JSON</span><textarea className={`${styles.codeArea} textArea`} value={workspace.importText} onChange={(event) => workspace.setImportText(event.target.value)} placeholder='{"items":[{"item_id":"Shoes_Loafers_01","display_name":"Loafers","slot":"shoes"}],"outfits":[]}' /></label>
                <div className="actionRow"><div className="chipRow"><span className="chip" data-tone="accent">merge upserts by id</span><span className="chip" data-tone="sun">replace rewrites registry</span></div><div className="chipRow"><button type="button" className="secondaryButton" onClick={() => void workspace.applyImport("merge")} disabled={workspace.state.mutating || !workspace.importText.trim()}>Merge import</button><button type="button" className="ghostButton" onClick={() => void workspace.applyImport("replace")} disabled={workspace.state.mutating || !workspace.importText.trim()}>Replace registry</button></div></div>
              </div>
            </article>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
