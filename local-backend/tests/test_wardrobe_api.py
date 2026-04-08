from __future__ import annotations

from pathlib import Path


def test_wardrobe_snapshot_seeds_taxonomy_and_registry_file(client, settings) -> None:
    response = client.get("/v1/wardrobe")

    assert response.status_code == 200
    payload = response.json()

    assert payload["summary"]["slot_count"] == 13
    assert payload["summary"]["reserved_slot_count"] == 3
    assert payload["summary"]["item_count"] >= 1
    assert any(slot["key"] == "hair" for slot in payload["slots"])
    assert any(item["item_id"] == "Hair_ShortA_01" for item in payload["items"])

    registry_path = Path(payload["registry_path"])
    assert registry_path == settings.data_dir / "wardrobe" / "registry.json"
    assert registry_path.exists()


def test_wardrobe_item_create_persists_and_reports_item_level_warnings(client) -> None:
    create_item = client.post(
        "/v1/wardrobe/items",
        json={
            "item_id": "Dress_School_01",
            "display_name": "School Dress",
            "slot": "dress",
            "occupies_slots": ["top", "bottom"],
            "blocks_slots": ["dress"],
            "compatible_tags": ["formal", "layered"],
            "incompatible_tags": ["oversized"],
            "hide_body_regions": ["TorsoUpper", "TorsoLower"],
        },
    )

    assert create_item.status_code == 200
    created = create_item.json()

    assert created["slot"] == "dress"
    assert created["sync_status"] == "needs_attention"
    assert any(issue["code"] == "blocks_own_slot" for issue in created["validation_issues"])

    snapshot = client.get("/v1/wardrobe")
    assert snapshot.status_code == 200
    items = snapshot.json()["items"]
    assert any(item["item_id"] == "Dress_School_01" for item in items)


def test_wardrobe_outfit_validation_reports_dress_with_separates_warning(client) -> None:
    dress_item = client.post(
        "/v1/wardrobe/items",
        json={
            "item_id": "Dress_Evening_01",
            "display_name": "Evening Dress",
            "slot": "dress",
            "compatible_tags": ["formal"],
        },
    )
    assert dress_item.status_code == 200

    top_item = client.post(
        "/v1/wardrobe/items",
        json={
            "item_id": "Top_Blouse_01",
            "display_name": "Soft Blouse",
            "slot": "top",
            "compatible_tags": ["formal"],
        },
    )
    assert top_item.status_code == 200

    create_outfit = client.post(
        "/v1/wardrobe/outfits",
        json={
            "outfit_id": "outfit_evening_mix",
            "display_name": "Evening Mix",
            "slot_assignments": {
                "dress": "Dress_Evening_01",
                "top": "Top_Blouse_01",
            },
        },
    )

    assert create_outfit.status_code == 200
    outfit = create_outfit.json()
    assert outfit["sync_status"] == "needs_attention"
    assert any(issue["code"] == "dress_with_separates" for issue in outfit["validation_issues"])


def test_wardrobe_export_and_replace_import_roundtrip(client) -> None:
    export_response = client.get("/v1/wardrobe/export")

    assert export_response.status_code == 200
    exported = export_response.json()
    assert exported["summary"]["item_count"] >= 1

    import_response = client.post(
        "/v1/wardrobe/import",
        json={
            "mode": "replace",
            "items": [
                {
                    "item_id": "Shoes_Loafers_01",
                    "display_name": "Loafers",
                    "slot": "shoes",
                    "compatible_tags": ["school"],
                }
            ],
            "outfits": [
                {
                    "outfit_id": "outfit_school",
                    "display_name": "School Uniform",
                    "slot_assignments": {
                        "shoes": "Shoes_Loafers_01",
                    },
                }
            ],
        },
    )

    assert import_response.status_code == 200
    snapshot = import_response.json()
    assert snapshot["summary"]["item_count"] == 1
    assert snapshot["summary"]["outfit_count"] == 1
    assert snapshot["items"][0]["item_id"] == "Shoes_Loafers_01"
    assert snapshot["outfits"][0]["outfit_id"] == "outfit_school"
