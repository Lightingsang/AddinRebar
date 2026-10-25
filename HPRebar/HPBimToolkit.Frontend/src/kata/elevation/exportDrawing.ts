// Saving and printing the drawing as it is on screen. Files go out as blob downloads; the add-in's WebView2 host shows
// a Save dialog for each (WebViewHost.OnDownloadStarting), so nothing lands silently in the Downloads folder.

/** The drawing as a standalone SVG document (namespaces, size, its own background). */
export function svgDocument(svg: SVGSVGElement): string {
  const copy = svg.cloneNode(true) as SVGSVGElement;
  copy.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
  copy.setAttribute('viewBox', `0 0 ${svg.width.baseVal.value} ${svg.height.baseVal.value}`);
  copy.removeAttribute('class');
  return `<?xml version="1.0" encoding="UTF-8"?>\n${new XMLSerializer().serializeToString(copy)}`;
}

export function download(name: string, blob: Blob): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = name;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

/** The SVG drawn into a canvas `factor` times larger (data URL image: the page's CSP allows data: images only). */
export async function pngOf(svgText: string, width: number, height: number, factor: number): Promise<Blob> {
  const image = new Image();
  image.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svgText)}`;
  await image.decode();
  const canvas = document.createElement('canvas');
  canvas.width = Math.round(width * factor);
  canvas.height = Math.round(height * factor);
  const context = canvas.getContext('2d');
  if (!context) throw new Error('canvas 2D is not available');
  context.drawImage(image, 0, 0, canvas.width, canvas.height);
  return new Promise((resolve, reject) => canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('PNG could not be written'))), 'image/png'));
}

/** Prints only the drawing (Chromium's print dialog, "Save as PDF" included): the rest of the page is hidden meanwhile. */
export function printDrawing(svgText: string): void {
  const sheet = document.createElement('div');
  sheet.className = 'kata-print-sheet';
  sheet.innerHTML = svgText.replace(/^<\?xml[^>]*>\s*/, '');
  const style = document.createElement('style');
  style.textContent =
    '@media print { body > *:not(.kata-print-sheet) { display: none !important; } .kata-print-sheet svg { width: 100%; height: auto; } }' +
    '@media screen { .kata-print-sheet { display: none; } } @page { size: landscape; margin: 10mm; }';
  document.body.append(style, sheet);
  const cleanUp = () => {
    sheet.remove();
    style.remove();
  };
  window.addEventListener('afterprint', cleanUp, { once: true });
  window.print();
}

/** A file name for the drawing: the beam name and a time stamp, characters Windows refuses replaced. */
export function fileName(beam: string, extension: string): string {
  const stamp = new Date().toISOString().slice(0, 16).replace(/[-:T]/g, '');
  const safe = (beam || 'dam').replace(/[\\/:*?"<>|\s]+/g, '_');
  return `Kata_${safe}_${stamp}.${extension}`;
}
