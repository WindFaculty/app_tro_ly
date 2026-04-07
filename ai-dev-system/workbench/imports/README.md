# Imports Workbench

Current implementation: imported source assets still live in root import folders.

Current source-of-truth material:

- `../../../Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`
- `../../../bleder/Meshy_AI_body_kokomi_0314010618_generate.fbx`

Notes:

- The Meshy kimono folder is a raw source import, not a runtime asset root.
- The FBX inside `bleder/` is currently an in-process authoring input rather than a normalized import destination.

Planned work:

- move raw source imports under `ai-dev-system/workbench/imports/` after structure validation and path cleanup are ready
