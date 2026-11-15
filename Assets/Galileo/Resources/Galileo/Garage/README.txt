Orbiter assembly bitmaps (the garage scene after the orbiter design).

All files here are painted on ONE canvas size (e.g. 1920x1080) with transparency, so laying them over each other
at the same size and position shows the finished probe. Each part is cut out automatically from its visible pixels.

  outline.png           silhouette of the whole probe, shown until every essential part is attached
  main_structure.png    main body, appears behind the parts once every essential part is attached
  <part id>.png         one file per part, named after "file" in garage.elements of content*.json:
      high_gain_antenna, rtg_1, rtg_2, magnetometer_boom, plasma_wave_antenna,
      scan_platform, propulsion_module, low_gain_antenna (optional part: not on the checklist, costs money)

Add, remove or rename parts in garage.elements (all three content files). x / y / w / h there are only used for the
placeholder rectangle while a part's bitmap is missing.
The garage background itself is Backgrounds/garage.png.
