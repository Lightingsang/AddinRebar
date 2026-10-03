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
  .lbl { background:rgba(0,0,0,.75); color:#fff; border:0; border-radius:3px; padding:1px 4px; font:11px 'Segoe UI', sans-serif; white-space:nowrap; }
  .svg-marker { background:transparent; border:none; }
  .leaflet-popup-content-wrapper { padding:0; overflow:hidden; border-radius:4px; box-shadow:0 3px 14px rgba(0,0,0,0.4); }
  .leaflet-popup-content { margin:0; line-height:1.4; }
  #msg { position:absolute; left:8px; bottom:8px; z-index:1000; font:11px 'Segoe UI', sans-serif; color:#ddd; background:rgba(0,0,0,.5); padding:2px 6px; border-radius:3px; }
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
  var lastBounds = [];
  map.on('zoomend', function () { post('zoom:' + map.getZoom()); });
  window.hpgeoUpdate = function (data) {
    layer.clearLayers();
    var bounds = [];
    var ptColor = data.pointColor || '#FFD400';
    var lnColor = data.lineColor || '#FF3030';
    (data.points || []).forEach(function (p) {
      bounds.push([p.lat, p.lon]);
      L.circleMarker([p.lat, p.lon], { radius: 4, color: '#333333', weight: 1.5, fillColor: '#FFFFFF', fillOpacity: 1.0 })
        .bindTooltip('<span style="color:' + ptColor + ';font-weight:bold;">' + p.label + '</span>', { permanent: true, direction: 'right', className: 'lbl', offset: [6, 0] }).addTo(layer);
    });
    (data.boundaries || []).forEach(function (b) {
      var latlngs = b.vertices.map(function (v) { bounds.push([v.lat, v.lon]); return [v.lat, v.lon]; });
      if (b.closed) L.polygon(latlngs, { color: lnColor, weight: 2, fillOpacity: 0.12 }).addTo(layer);
      else L.polyline(latlngs, { color: lnColor, weight: 2 }).addTo(layer);
    });
    if (data.exportBoundaryVertices && data.boundaryVertices && data.boundaryVertices.length > 0) {
      var style = data.markerStyle || 'triangle';
      data.boundaryVertices.forEach(function (bv) {
        bounds.push([bv.lat, bv.lon]);
        var icon;
        if (style === 'pushpin') {
          var pinSvg = '<svg width="22" height="26" viewBox="0 0 24 24" style="filter:drop-shadow(1px 2px 2px rgba(0,0,0,0.5));">' +
            '<path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7z" fill="#FFFFFF" stroke="#333333" stroke-width="1.5"/>' +
            '<circle cx="12" cy="9" r="2.5" fill="#333333"/>' +
            '</svg>';
          icon = L.divIcon({ html: pinSvg, className: 'svg-marker', iconSize: [22, 26], iconAnchor: [11, 26], popupAnchor: [0, -24] });
        } else if (style === 'circle') {
          var circleSvg = '<svg width="18" height="18" viewBox="0 0 24 24" style="filter:drop-shadow(1px 1px 2px rgba(0,0,0,0.5));">' +
            '<circle cx="12" cy="12" r="8" fill="#FFFFFF" fill-opacity="1.0" stroke="#333333" stroke-width="2"/>' +
            '<circle cx="12" cy="12" r="3" fill="#333333"/>' +
            '</svg>';
          icon = L.divIcon({ html: circleSvg, className: 'svg-marker', iconSize: [18, 18], iconAnchor: [9, 9], popupAnchor: [0, -9] });
        } else if (style === 'labelOnly') {
          var dotSvg = '<svg width="8" height="8" viewBox="0 0 8 8"><circle cx="4" cy="4" r="3" fill="#FFFFFF" stroke="#333333" stroke-width="1"/></svg>';
          icon = L.divIcon({ html: dotSvg, className: 'svg-marker', iconSize: [8, 8], iconAnchor: [4, 4], popupAnchor: [0, -4] });
        } else {
          var triSvg = '<svg width="20" height="20" viewBox="0 0 24 24" style="filter:drop-shadow(1px 1px 2px rgba(0,0,0,0.5));">' +
            '<polygon points="12,2 22,20 2,20" fill="#FFFFFF" fill-opacity="1.0" stroke="#333333" stroke-width="2"/>' +
            '<circle cx="12" cy="14" r="2.5" fill="#333333"/>' +
            '</svg>';
          icon = L.divIcon({ html: triSvg, className: 'svg-marker', iconSize: [20, 20], iconAnchor: [10, 10], popupAnchor: [0, -10] });
        }
        var marker = L.marker([bv.lat, bv.lon], { icon: icon });
        marker.bindTooltip('<span style="color:' + ptColor + ';font-weight:bold;">' + bv.label + '</span>', { permanent: true, direction: 'right', className: 'lbl', offset: [8, 0] });
        if (bv.popupHtml) {
          marker.bindPopup(bv.popupHtml, { maxWidth: 320 });
        }
        marker.addTo(layer);
      });
    }
    lastBounds = bounds;
    if (bounds.length === 1) map.setView(bounds[0], 18);
    else if (bounds.length > 1) map.fitBounds(bounds, { padding: [24, 24], maxZoom: 19 });
    post('zoom:' + map.getZoom());
  };
  window.chrome.webview.addEventListener('message', function (e) {
    if (e.data === 'fit-bounds') {
      if (lastBounds && lastBounds.length > 0) {
        if (lastBounds.length === 1) map.setView(lastBounds[0], 18);
        else map.fitBounds(lastBounds, { padding: [24, 24], maxZoom: 19 });
      }
      return;
    }
    try { window.hpgeoUpdate(typeof e.data === 'string' ? JSON.parse(e.data) : e.data); } catch (err) { post('update-error: ' + err); }
  });
  post('page-ready');
})();
</script></body></html>
""";
}
