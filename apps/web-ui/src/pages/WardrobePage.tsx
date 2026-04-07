import { PageTemplate } from "@/components/PageTemplate";

export function WardrobePage() {
  return (
    <PageTemplate
      title="Wardrobe"
      icon="WD"
      eyebrow="Taxonomy shell"
      description="A05 gives the wardrobe lane a real module shell so A13 can land on top of a stable desktop structure without waiting on live Unity preview."
      highlights={[
        {
          label: "Owner",
          value: "React",
          detail: "Forms, metadata, filters, presets",
        },
        {
          label: "Preview",
          value: "Unity",
          detail: "Avatar render and fitting feedback later",
        },
        {
          label: "Bridge",
          value: "typed",
          detail: "Equip intents stay contract-first",
        },
      ]}
    >
      <div className="appStack">
        <article className="surface">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Ownership split</span>
              <h3 className="surfaceTitle">Desktop workflow first, avatar preview second</h3>
              <p className="surfaceIntro">
                Wardrobe remains a desktop data module during Workstream A. The shell already
                frames item metadata, compatibility, and preset flows without claiming Unity sync is
                live.
              </p>
            </div>
          </div>
        </article>

        <div className="surfaceGrid">
          <article className="surface surfaceMuted">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Registry model</span>
                <h3 className="surfaceTitle">Core data slices</h3>
              </div>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone="accent">
                slots
              </span>
              <span className="chip" data-tone="warm">
                tags
              </span>
              <span className="chip" data-tone="sun">
                presets
              </span>
              <span className="chip" data-tone="success">
                compatibility
              </span>
            </div>
          </article>

          <article className="surface surfaceMuted">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Planned bridge</span>
                <h3 className="surfaceTitle">Future contract surfaces</h3>
              </div>
            </div>
            <div className="listStack">
              <div className="listRow">
                <div>
                  <p className="listTitle">wardrobe.equipItem</p>
                  <p className="listSubtitle">Desktop sends a typed equip intent to the runtime bridge.</p>
                </div>
                <span className="chip" data-tone="accent">
                  phase 6
                </span>
              </div>
              <div className="listRow">
                <div>
                  <p className="listTitle">avatar.stateChanged</p>
                  <p className="listSubtitle">Unity reports fit, pose, and preview feedback back to the shell.</p>
                </div>
                <span className="chip" data-tone="sun">
                  phase 6
                </span>
              </div>
            </div>
          </article>
        </div>

        <div className="emptyState">
          <p className="emptyStateTitle">A13 will populate this lane with real item data</p>
          <p className="emptyStateText">
            The design system pass only establishes the shell framing, card vocabulary, and boundary
            copy needed for later taxonomy and preset work.
          </p>
        </div>
      </div>
    </PageTemplate>
  );
}
