// Revit's ViewCube for the 3D view: a cube whose 6 faces, 12 edges and 8 corners turn the view to look from that side
// when clicked, light up under the cursor, and orbit the view when dragged; a compass ring under it (N/E/S/W letters
// turn to that side, dragging the ring turns about the vertical only). It draws itself in its own small canvas, oriented
// like the main camera, and only when asked (sync) or hovered.
import {
  BoxGeometry,
  CanvasTexture,
  Color,
  DoubleSide,
  EdgesGeometry,
  Group,
  LineBasicMaterial,
  LineSegments,
  Matrix4,
  Mesh,
  MeshBasicMaterial,
  OrthographicCamera,
  PlaneGeometry,
  Raycaster,
  RingGeometry,
  Scene,
  SRGBColorSpace,
  Sprite,
  SpriteMaterial,
  Vector2,
  Vector3,
  WebGLRenderer,
  type Camera,
} from 'three';
import { COMPASS, regionAt, regionName, type CubeRegion } from './revitNavigation';

export interface ViewCubeHandlers {
  /** A face, edge, corner or compass letter was clicked. */
  goTo(region: CubeRegion): void;
  /** The cube was dragged: radians about the vertical and up (as the main view's Shift + middle drag). */
  orbit(azimuth: number, elevation: number): void;
  /** A drag of the cube ended (the pivot ball goes away). */
  orbitEnd(): void;
}

const FACES: { normal: Vector3; u: Vector3; v: Vector3; label: string }[] = [
  { normal: new Vector3(0, 0, 1), u: new Vector3(1, 0, 0), v: new Vector3(0, 1, 0), label: 'TOP' },
  { normal: new Vector3(0, 0, -1), u: new Vector3(1, 0, 0), v: new Vector3(0, -1, 0), label: 'BOTTOM' },
  { normal: new Vector3(0, -1, 0), u: new Vector3(1, 0, 0), v: new Vector3(0, 0, 1), label: 'FRONT' },
  { normal: new Vector3(0, 1, 0), u: new Vector3(-1, 0, 0), v: new Vector3(0, 0, 1), label: 'BACK' },
  { normal: new Vector3(1, 0, 0), u: new Vector3(0, 1, 0), v: new Vector3(0, 0, 1), label: 'RIGHT' },
  { normal: new Vector3(-1, 0, 0), u: new Vector3(0, -1, 0), v: new Vector3(0, 0, 1), label: 'LEFT' },
];
/** Tile centres and sizes across a face: an edge band, the middle, an edge band (Revit's proportions). */
const BANDS: { at: number; size: number }[] = [
  { at: -0.4, size: 0.2 },
  { at: 0, size: 0.6 },
  { at: 0.4, size: 0.2 },
];
const LIGHT = new Vector3(-0.35, -0.55, 0.8).normalize();
const HOVER = new Color('#7db8ec');
const RING_Z = -0.62;
const ORBIT_PER_PX = 0.009;
const CLICK_PX = 3;

const key = (r: CubeRegion) => r.join(',');

function labelTexture(text: string, fill: string, ink: string): CanvasTexture {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = 256;
  const g = canvas.getContext('2d')!;
  g.fillStyle = fill;
  g.fillRect(0, 0, 256, 256);
  g.fillStyle = ink;
  // As large as the face allows, as on Revit's cube (BOTTOM is the widest word).
  g.font = `bold ${text.length > 5 ? 62 : text.length > 4 ? 72 : 84}px "Segoe UI", Arial, sans-serif`;
  g.textAlign = 'center';
  g.textBaseline = 'middle';
  g.fillText(text, 128, 132);
  const texture = new CanvasTexture(canvas);
  texture.colorSpace = SRGBColorSpace;
  texture.anisotropy = 4;
  return texture;
}

function letterSprite(letter: string): Sprite {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = 64;
  const g = canvas.getContext('2d')!;
  g.fillStyle = '#2b2f35';
  g.font = 'bold 44px "Segoe UI", Arial, sans-serif';
  g.textAlign = 'center';
  g.textBaseline = 'middle';
  g.fillText(letter, 32, 35);
  const texture = new CanvasTexture(canvas);
  texture.colorSpace = SRGBColorSpace;
  const sprite = new Sprite(new SpriteMaterial({ map: texture, transparent: true }));
  sprite.scale.set(0.36, 0.36, 1);
  return sprite;
}

export class ViewCube {
  readonly element: HTMLCanvasElement;
  private readonly renderer: WebGLRenderer;
  private readonly scene = new Scene();
  private readonly camera = new OrthographicCamera(-1.18, 1.18, 1.18, -1.18, 0.1, 20);
  private readonly tiles: Mesh[] = [];
  private readonly byRegion = new Map<string, Mesh[]>();
  private readonly letters: Sprite[] = [];
  private readonly ring: Mesh;
  private readonly ray = new Raycaster();
  private hovered: string | null = null;
  private press: { x: number; y: number; moved: boolean; region: CubeRegion | null; ring: boolean } | null = null;

  constructor(
    private readonly size: number,
    private readonly handlers: ViewCubeHandlers,
  ) {
    this.renderer = new WebGLRenderer({ antialias: true, alpha: true });
    this.renderer.setPixelRatio(window.devicePixelRatio);
    this.renderer.setSize(size, size);
    this.element = this.renderer.domElement;
    this.element.style.display = 'block';
    this.element.style.cursor = 'default';
    this.element.dataset.testid = 'kata-viewcube';

    const cube = new Group();
    for (const face of FACES) {
      const shade = 0.8 + 0.2 * Math.max(0, face.normal.dot(LIGHT));
      const fill = new Color().setScalar(0.93 * shade).getStyle();
      for (const bu of BANDS)
        for (const bv of BANDS) {
          const centre = bu.at === 0 && bv.at === 0;
          const material = centre ? new MeshBasicMaterial({ map: labelTexture(face.label, fill, '#2e3238') }) : new MeshBasicMaterial({ color: new Color(fill) });
          const tile = new Mesh(new PlaneGeometry(bu.size, bv.size), material);
          const position = face.normal.clone().multiplyScalar(0.5).addScaledVector(face.u, bu.at).addScaledVector(face.v, bv.at);
          tile.applyMatrix4(new Matrix4().makeBasis(face.u, face.v, face.normal).setPosition(position));
          const region = regionAt(position);
          tile.userData = { region, base: centre ? new Color('#ffffff') : new Color(fill) };
          this.tiles.push(tile);
          const list = this.byRegion.get(key(region)) ?? [];
          list.push(tile);
          this.byRegion.set(key(region), list);
          cube.add(tile);
        }
    }
    cube.add(new LineSegments(new EdgesGeometry(new BoxGeometry(1.002, 1.002, 1.002)), new LineBasicMaterial({ color: 0x5c626b })));
    this.scene.add(cube);

    this.ring = new Mesh(new RingGeometry(0.8, 1.02, 72), new MeshBasicMaterial({ color: 0xc9ccd1, transparent: true, opacity: 0.85, side: DoubleSide }));
    this.ring.position.z = RING_Z;
    // The letters are drawn after the translucent ring (and are hidden only by the cube), as on Revit's compass.
    (this.ring.material as MeshBasicMaterial).depthWrite = false;
    this.scene.add(this.ring);
    for (const { letter, region } of COMPASS) {
      const sprite = letterSprite(letter);
      sprite.position.set(region[0] * 0.91, region[1] * 0.91, RING_Z + 0.04);
      sprite.userData = { region };
      sprite.renderOrder = 1;
      this.letters.push(sprite);
      this.scene.add(sprite);
    }

    this.element.addEventListener('pointerdown', this.onDown);
    this.element.addEventListener('pointermove', this.onMove);
    this.element.addEventListener('pointerup', this.onUp);
    this.element.addEventListener('pointerleave', this.onLeave);
    this.element.addEventListener('pointercancel', this.onCancel);
  }

  /** Turns the cube to the main camera's orientation and draws it. */
  sync(main: Camera): void {
    this.camera.quaternion.copy(main.quaternion);
    this.camera.position.set(0, 0, 6).applyQuaternion(main.quaternion);
    this.camera.updateMatrixWorld();
    this.draw();
  }

  /** The faces facing the camera and where their centres are on the canvas (CSS px): what a harness clicks. */
  visibleFaces(): Record<string, [number, number]> {
    const out: Record<string, [number, number]> = {};
    const toward = new Vector3(0, 0, 1).applyQuaternion(this.camera.quaternion);
    for (const face of FACES) {
      if (face.normal.dot(toward) < 0.2) continue;
      const p = face.normal.clone().multiplyScalar(0.5).project(this.camera);
      out[face.label] = [Math.round(((p.x + 1) / 2) * this.size), Math.round(((1 - p.y) / 2) * this.size)];
    }
    return out;
  }

  dispose(): void {
    this.element.removeEventListener('pointerdown', this.onDown);
    this.element.removeEventListener('pointermove', this.onMove);
    this.element.removeEventListener('pointerup', this.onUp);
    this.element.removeEventListener('pointerleave', this.onLeave);
    this.element.removeEventListener('pointercancel', this.onCancel);
    this.scene.traverse((o) => {
      if (o instanceof Mesh || o instanceof LineSegments || o instanceof Sprite) {
        o.geometry?.dispose();
        const material = o.material as MeshBasicMaterial;
        material.map?.dispose();
        material.dispose();
      }
    });
    this.renderer.forceContextLoss();
    this.renderer.dispose();
    this.element.remove();
  }

  private draw(): void {
    this.element.dataset.faces = JSON.stringify(this.visibleFaces());
    this.renderer.render(this.scene, this.camera);
  }

  /** What is under a canvas point: a region of the cube or a compass letter, the ring, or nothing. */
  private pick(e: PointerEvent): { region: CubeRegion | null; ring: boolean } {
    const rect = this.element.getBoundingClientRect();
    this.ray.setFromCamera(new Vector2(((e.clientX - rect.left) / rect.width) * 2 - 1, -((e.clientY - rect.top) / rect.height) * 2 + 1), this.camera);
    const hit = this.ray.intersectObjects([...this.tiles, ...this.letters, this.ring], false)[0];
    if (!hit) return { region: null, ring: false };
    if (hit.object === this.ring) return { region: null, ring: true };
    return { region: hit.object.userData.region as CubeRegion, ring: false };
  }

  private highlight(region: CubeRegion | null): void {
    const next = region ? key(region) : null;
    if (next === this.hovered) return;
    for (const tile of this.tiles) (tile.material as MeshBasicMaterial).color.copy(tile.userData.base as Color);
    if (next) for (const tile of this.byRegion.get(next) ?? []) (tile.material as MeshBasicMaterial).color.copy(HOVER);
    for (const sprite of this.letters) (sprite.material as SpriteMaterial).color.set(next === key(sprite.userData.region as CubeRegion) ? HOVER : 0xffffff);
    this.hovered = next;
    this.element.title = region ? regionName(region) : '';
    this.draw();
  }

  private readonly onDown = (e: PointerEvent) => {
    if (e.button !== 0) return;
    e.stopPropagation();
    this.element.setPointerCapture(e.pointerId);
    const { region, ring } = this.pick(e);
    this.press = { x: e.clientX, y: e.clientY, moved: false, region, ring };
  };

  private readonly onMove = (e: PointerEvent) => {
    const press = this.press;
    if (press) {
      const dx = e.clientX - press.x;
      const dy = e.clientY - press.y;
      if (!press.moved && Math.hypot(dx, dy) < CLICK_PX) return;
      press.moved = true;
      press.x = e.clientX;
      press.y = e.clientY;
      // The ring only turns about the vertical; the cube orbits both ways.
      this.handlers.orbit(-dx * ORBIT_PER_PX, press.ring ? 0 : dy * ORBIT_PER_PX);
      return;
    }
    const { region, ring } = this.pick(e);
    this.element.style.cursor = region || ring ? 'pointer' : 'default';
    this.highlight(region);
  };

  private readonly onUp = (e: PointerEvent) => {
    const press = this.press;
    this.press = null;
    if (press?.moved) this.handlers.orbitEnd();
    if (!press || press.moved || !press.region) return;
    e.stopPropagation();
    this.handlers.goTo(press.region);
  };

  private readonly onCancel = () => {
    if (this.press?.moved) this.handlers.orbitEnd();
    this.press = null;
  };

  private readonly onLeave = () => {
    if (!this.press) this.highlight(null);
  };
}
