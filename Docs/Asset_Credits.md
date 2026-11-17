# Seghani Tak asset sources

| Asset | Source and use |
| --- | --- |
| Room architecture, shelter furniture, and hazard models | Geometry and materials built in the project using Unity primitives and generated textures. Visual detail uses separate render meshes where the props need simple physics colliders. |
| HUD, menus, and help illustrations | Text and shapes built in the project with Unity UI and its bundled Legacy Runtime font. |
| Rumble, hazard warnings, impacts, and game feedback | Audio generated in code. The project contains no imported audio clips. |
| Engine and UI packages | Unity and the packages declared in `Packages/manifest.json`, distributed under their respective terms. |

The draft runtime asset inventory contains no imported third-party model, texture, or sound files. For any external assets added later, record the asset name, creator, source URL, licence, and modifications before distribution.
