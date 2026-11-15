Orbiter assembly bitmaps (the garage scene after the orbiter design).

All files here are painted on ONE canvas size (e.g. 1920x1080) with transparency, so laying them over each other
at the same size and position shows the finished probe. Each part is cut out automatically from its visible pixels.

  outline.png           silhouette of the whole probe, shown until every essential part is attached
  main_structure.png    main body, appears behind the parts once every essential part is attached
  <part id>.png         one file per part, named after "file" in garage.elements of content*.json:
      retro_propulsion_module, radioisotope_thermoelectric_generator, magnetometer_sensors,
      attitude_control_thrusters, high_gain_antenna,
      lowgain_antenna (optional part: not on the checklist, costs money)
  The order of garage.elements is the layer order: later parts are drawn on top.

Where each loose part lies in the garage is homeX / homeY in garage.elements: a point on the garage picture
(Backgrounds/garage.png), 0..1 from the left and from the bottom. The part's solid pixels are centred there.

Add, remove or rename parts in garage.elements (all three content files). x / y / w / h there are only used for the
placeholder rectangle while a part's bitmap is missing.
The garage background itself is Backgrounds/garage.png.
