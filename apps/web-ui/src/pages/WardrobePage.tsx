import { PageTemplate } from "@/components/PageTemplate";

export function WardrobePage() {
  return (
    <PageTemplate title="Wardrobe" icon="🧥">
      <div className="stack">
        <div className="card">
          <p className="eyebrow">Ownership split</p>
          <h3 className="sectionTitle">React owns workflow, Unity owns preview</h3>
          <p className="bodyText">
            Page này đánh dấu boundary đã chốt ở Phase 1: React sẽ chịu form, filter,
            metadata và equip intent; Unity chỉ chịu avatar render, animation và fitting
            feedback ở vùng 3D trung tâm.
          </p>
        </div>

        <div className="card">
          <h3 className="sectionTitle">Planned commands</h3>
          <div className="listStack">
            <div className="listRow">
              <div>
                <p className="listTitle">wardrobe.equipItem</p>
                <p className="helperText">React gửi intent đổi item sang Unity bridge</p>
              </div>
              <span className="pill">Phase 6</span>
            </div>
            <div className="listRow">
              <div>
                <p className="listTitle">avatar.stateChanged</p>
                <p className="helperText">Unity phản hồi trạng thái mặc đồ và animation</p>
              </div>
              <span className="pill">Phase 6</span>
            </div>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
