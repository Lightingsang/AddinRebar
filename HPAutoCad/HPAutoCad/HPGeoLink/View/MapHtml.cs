namespace HPAutoCad.HPGeoLink.View;

/// <summary>
/// The page the map panel loads: Leaflet over Esri World Imagery tiles (both from the internet — the panel is a
/// convenience, the Google Earth / Maps buttons are the fail-safe). Satellite imagery, not a street map: a
/// surveyor checks a plot against what is on the ground. OpenStreetMap tiles were tried first and answer
/// 403 "access blocked" to a WebView2 page loaded from a string (no referer, generic user agent). The host pushes a JSON document through
/// <c>chrome.webview.postMessage</c>; the page answers <c>tile-loaded</c> once the first tile has drawn and
/// <c>tile-error</c> when the tile server is unreachable, so the add-in can log the spike gate and hide the panel.
/// </summary>
internal static class MapHtml
{
    public const string LeafletVersion = "1.9.4";
    // Subresource integrity of the pinned files (sha384 computed 2026-09-18): a tampered CDN loads nothing, and the
    // page then reports leaflet-missing instead of running foreign script.
    public const string LeafletJsSri = "sha384-cxOPjt7s7Iz04uaHJceBmS+qpjv2JkIHNVcuOrM+YHwZOmJGBXI00mdUXEq65HTH";
    public const string LeafletCssSri = "sha384-sHL9NAb7lN7rfvG5lfHpm643Xkcjzp4jFvuavGOndn6pjVqS6ny56CAt3nsEVT4H";

    public static string Page(bool dark) => $$"""
<!doctype html>
<html><head><meta charset="utf-8"/>
<meta http-equiv="X-UA-Compatible" content="IE=edge"/>
<link rel="stylesheet" href="https://unpkg.com/leaflet@{{LeafletVersion}}/dist/leaflet.css" integrity="{{LeafletCssSri}}" crossorigin="anonymous"/>
<script src="https://unpkg.com/leaflet@{{LeafletVersion}}/dist/leaflet.js" integrity="{{LeafletJsSri}}" crossorigin="anonymous"></script>
<style>
  html, body, #map { margin:0; height:100%; width:100%; background:{{(dark ? "#1F1F1F" : "#F3F3F3")}}; }
  .lbl { background:rgba(0,0,0,.65); color:#fff; border:0; border-radius:3px; padding:1px 4px; font:11px Segoe UI, sans-serif; white-space:nowrap; }
  #msg { position:absolute; left:8px; bottom:8px; z-index:1000; font:11px Segoe UI, sans-serif; color:#ddd; background:rgba(0,0,0,.5); padding:2px 6px; border-radius:3px; }
</style></head>
<body><div id="map"></div><div id="msg">HPGeo map</div>
<script>
(function () {
  var post = function (m) { try { window.chrome.webview.postMessage(m); } catch (e) {} };
  if (typeof L === 'undefined') { document.getElementById('msg').textContent = 'Leaflet không tải được (cần Internet)'; post('leaflet-missing'); return; }
  var map = L.map('map', { zoomControl: true, attributionControl: true }).setView([16.0, 106.0], 5);
  var tiles = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', { maxZoom: 19, attribution: 'Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community' });
  var first = true;
  tiles.on('tileload', function () { if (first) { first = false; post('tile-loaded'); document.getElementById('msg').textContent = 'Esri World Imagery'; } });
  tiles.on('tileerror', function () { if (first) { first = false; post('tile-error'); document.getElementById('msg').textContent = 'Không tải được bản đồ nền (Internet?)'; } });
  tiles.addTo(map);
  var layer = L.layerGroup().addTo(map);
  var labelColor = '#FFFFFF';
  window.hpgeoUpdate = function (data) {
    layer.clearLayers();
    var bounds = [];
    (data.points || []).forEach(function (p) {
      bounds.push([p.lat, p.lon]);
      L.circleMarker([p.lat, p.lon], { radius: 4, color: '#FFD400', weight: 2, fillColor: '#FFD400', fillOpacity: 0.9 })
        .bindTooltip(String(p.label), { permanent: true, direction: 'right', className: 'lbl', offset: [6, 0] }).addTo(layer);
    });
    (data.boundaries || []).forEach(function (b) {
      var latlngs = b.vertices.map(function (v) { bounds.push([v.lat, v.lon]); return [v.lat, v.lon]; });
      if (b.closed) L.polygon(latlngs, { color: '#FF3030', weight: 2, fillOpacity: 0.12 }).addTo(layer);
      else L.polyline(latlngs, { color: '#FF3030', weight: 2 }).addTo(layer);
    });
    if (bounds.length === 1) map.setView(bounds[0], 18);
    else if (bounds.length > 1) map.fitBounds(bounds, { padding: [24, 24], maxZoom: 19 });
  };
  window.chrome.webview.addEventListener('message', function (e) {
    try { window.hpgeoUpdate(typeof e.data === 'string' ? JSON.parse(e.data) : e.data); } catch (err) { post('update-error: ' + err); }
  });
  post('page-ready');
})();
</script></body></html>
""";
}
