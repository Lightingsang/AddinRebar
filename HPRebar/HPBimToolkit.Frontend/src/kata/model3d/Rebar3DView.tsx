// The 3D view of the plan (three.js, its own chunk): bars as tubes instanced per shape, the concrete translucent,
// navigated as Revit's 3D view — wheel zooms about the cursor, middle button pans, Shift + middle orbits about the
// selection (or the model), a middle double click fits the model, and a ViewCube with its compass, Home and menu.
// Renders on demand, never in a loop. A click selects a bar group by the key the elevation uses: Delete removes it,
// Ctrl+Z undoes, Esc clears it, then closes the window as the elevation does.
import { useEffect, useRef, useState, type KeyboardEvent, type PointerEvent } from 'react';
import {
  Color,
  DirectionalLight,
  EdgesGeometry,
  Group,
  HemisphereLight,
  InstancedMesh,
  LineBasicMaterial,
  LineSegments,
  Matrix4,
  Mesh,
  MeshStandardMaterial,
  Raycaster,
  Scene,
  Vector2,
  Vector3,
  WebGLRenderer,
} from 'three';
import { ChevronDown, House } from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import type { KataTheme } from '../kataContracts';
import type { KataBarRole, KataWeb3dGroup, KataWeb3dModel } from '../kataScene';
import { CameraRig, type Projection } from './cameraRig';
import { useT } from '../i18n';
import { HOVER_COLOR, SELECTED_COLOR, barGeometry, boxGeometry, describeGroup, isStirrup, modelBounds, roleColor } from './model3dGeometry';
import { HOME_REGION, regionDirection, regionName, regionOfDirection } from './revitNavigation';
import { ViewCube } from './viewCube';

const CLICK_PX = 4;
const CUBE_PX = 120;
const ORBIT_PER_PX = 0.008;
const DOUBLE_CLICK_MS = 350;
/** The second middle press of a double click lands this close to the first (CSS px). */
const DOUBLE_CLICK_PX = 4;

export interface Rebar3DViewProps {
  model: KataWeb3dModel;
  theme: KataTheme;
  /** The selected bar group (shared with the elevation), null when nothing is. */
  selectedKeys: string[] | null;
  onSelectKeys(keys: string[] | null): void;
  removeBars(keys: string[]): void;
  canUndoRemoveBars: boolean;
  undoRemoveBars(): void;
  onUnhandledEscape(): void;
}

interface World {
  renderer: WebGLRenderer;
  scene: Scene;
  rig: CameraRig;
  cube: ViewCube;
  render(): void;
  content: Group | null;
  bars: InstancedMesh[];
  concrete: Group | null;
  /** The direction Home turns to (Revit's "Set Current View as Home" changes it). */
  home: Vector3;
}

export default function Rebar3DView(props: Rebar3DViewProps) {
  const { model, theme, selectedKeys } = props;
  const box = useRef<HTMLDivElement>(null);
  const cubeBox = useRef<HTMLDivElement>(null);
  const world = useRef<World | null>(null);
  const pivotRef = useRef<() => Vector3>(() => new Vector3());
  const [failure, setFailure] = useState<string | null>(null);
  const [showConcrete, setShowConcrete] = useState(true);
  const [showStirrups, setShowStirrups] = useState(true);
  const [projection, setProjection] = useState<Projection>('orthographic');
  const [hover, setHover] = useState<{ group: KataWeb3dGroup; x: number; y: number } | null>(null);
  const [renderMs, setRenderMs] = useState(0);
  const press = useRef<{ x: number; y: number } | null>(null);
  const dark = theme === 'dark';

  // What Shift + middle orbits about: the selected bars, or the whole model.
  pivotRef.current = () => {
    const w = world.current;
    const selected = new Set(selectedKeys ?? []);
    const groups = model.groups.filter((g) => g.key !== '' && selected.has(g.key));
    if (!w) return new Vector3();
    if (groups.length === 0) return w.rig.modelCentre();
    const b = modelBounds({ ...model, groups, concrete: [] });
    return new Vector3((b.min.x + b.max.x) / 2, (b.min.y + b.max.y) / 2, (b.min.z + b.max.z) / 2);
  };

  // The renderer, cameras, ViewCube and mouse handling live as long as the view.
  useEffect(() => {
    const element = box.current!;
    let renderer: WebGLRenderer;
    try {
      renderer = new WebGLRenderer({ antialias: true });
    } catch (error) {
      setFailure(error instanceof Error ? error.message : String(error));
      return;
    }
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.domElement.style.display = 'block';
    element.prepend(renderer.domElement);

    const scene = new Scene();
    scene.add(new HemisphereLight(0xffffff, 0x555555, 1.6));
    const sun = new DirectionalLight(0xffffff, 1.4);
    sun.position.set(-0.4, -0.6, 1);
    scene.add(sun);

    const w = { renderer, scene, content: null, bars: [], concrete: null, home: regionDirection(HOME_REGION) } as unknown as World;
    w.render = () => {
      const started = performance.now();
      renderer.render(scene, w.rig.camera);
      setRenderMs(performance.now() - started);
      w.cube?.sync(w.rig.camera);
      const region = regionOfDirection(w.rig.direction());
      element.dataset.view = region ? regionName(region) : '';
    };
    w.rig = new CameraRig(renderer.domElement, scene, () => w.render());
    w.cube = new ViewCube(CUBE_PX, {
      goTo: (region) => w.rig.goTo(region),
      orbit: (azimuth, elevation) => w.rig.orbit(azimuth, elevation, pivotRef.current()),
      orbitEnd: () => w.rig.hidePivot(),
    });
    cubeBox.current?.prepend(w.cube.element);
    world.current = w;

    // Shift + middle drag orbits about the pivot; a middle double click fits the model (Revit). Caught before
    // OrbitControls, which keeps the plain middle drag (pan) and the wheel (zoom about the cursor).
    let orbiting: { x: number; y: number; pivot: Vector3 } | null = null;
    let lastMiddle = { time: -Infinity, x: 0, y: 0 };
    const onDown = (e: globalThis.PointerEvent) => {
      if (e.button !== 1) return;
      e.preventDefault(); // no autoscroll
      if (e.shiftKey) {
        e.stopPropagation();
        orbiting = { x: e.clientX, y: e.clientY, pivot: pivotRef.current() };
        element.setPointerCapture(e.pointerId);
        return;
      }
      if (e.timeStamp - lastMiddle.time < DOUBLE_CLICK_MS && Math.hypot(e.clientX - lastMiddle.x, e.clientY - lastMiddle.y) <= DOUBLE_CLICK_PX) {
        e.stopPropagation();
        w.rig.fit();
        lastMiddle = { time: -Infinity, x: 0, y: 0 };
        return;
      }
      lastMiddle = { time: e.timeStamp, x: e.clientX, y: e.clientY };
    };
    const onMove = (e: globalThis.PointerEvent) => {
      if (!orbiting) return;
      w.rig.orbit(-(e.clientX - orbiting.x) * ORBIT_PER_PX, (e.clientY - orbiting.y) * ORBIT_PER_PX, orbiting.pivot);
      orbiting.x = e.clientX;
      orbiting.y = e.clientY;
    };
    const onUp = () => {
      if (!orbiting) return;
      orbiting = null;
      w.rig.hidePivot();
    };
    element.addEventListener('pointerdown', onDown, { capture: true });
    element.addEventListener('pointermove', onMove);
    element.addEventListener('pointerup', onUp);

    const resize = () => {
      const width = Math.max(1, element.clientWidth);
      const height = Math.max(1, element.clientHeight);
      renderer.setSize(width, height);
      w.rig.setAspect(width / height);
      w.render();
    };
    const observer = new ResizeObserver(resize);
    observer.observe(element);
    resize();

    return () => {
      observer.disconnect();
      element.removeEventListener('pointerdown', onDown, { capture: true });
      element.removeEventListener('pointermove', onMove);
      element.removeEventListener('pointerup', onUp);
      w.cube.dispose();
      w.rig.dispose();
      disposeContent(w);
      renderer.forceContextLoss();
      renderer.dispose();
      renderer.domElement.remove();
      world.current = null;
    };
  }, []);

  // The model: rebuilt when the plan changes (a removal sends a new one).
  useEffect(() => {
    const w = world.current;
    if (!w) return;
    const first = w.content === null;
    disposeContent(w);

    const content = new Group();
    w.bars = model.groups.map((group, index) => {
      const mesh = new InstancedMesh(barGeometry(group), new MeshStandardMaterial({ roughness: 0.55, metalness: 0.25 }), group.offsets.length);
      group.offsets.forEach((offset, i) => mesh.setMatrixAt(i, new Matrix4().makeTranslation(offset, 0, 0)));
      mesh.instanceMatrix.needsUpdate = true;
      mesh.computeBoundingSphere();
      mesh.userData = { index };
      content.add(mesh);
      return mesh;
    });

    const concrete = new Group();
    const surface = new MeshStandardMaterial({ color: 0xa0a4aa, transparent: true, opacity: 0.14, depthWrite: false });
    const edges = new LineBasicMaterial({ color: 0x8a9099, transparent: true, opacity: 0.55 });
    for (const b of model.concrete) {
      const geometry = boxGeometry(b);
      concrete.add(new Mesh(geometry, surface));
      concrete.add(new LineSegments(new EdgesGeometry(geometry), edges));
    }
    content.add(concrete);
    w.concrete = concrete;
    w.content = content;
    w.scene.add(content);
    w.rig.setBounds(modelBounds(model));

    if (first) w.rig.fit(w.home, true);
    w.render();
  }, [model]);

  // Colours, visibility and the background follow the theme, the selection, the hover and the toggles.
  useEffect(() => {
    const w = world.current;
    if (!w) return;
    const selected = new Set(selectedKeys ?? []);
    w.scene.background = new Color(dark ? 0x1f2125 : 0xf3f4f6);
    w.bars.forEach((mesh, i) => {
      const group = model.groups[i]!;
      const isSelected = group.key !== '' && selected.has(group.key);
      const isHover = hover?.group === group && group.key !== '';
      (mesh.material as MeshStandardMaterial).color.setHex(isSelected ? SELECTED_COLOR : isHover ? HOVER_COLOR : roleColor(group.role, dark));
      mesh.visible = showStirrups || !isStirrup(group.role) || isSelected;
    });
    if (w.concrete) w.concrete.visible = showConcrete;
    w.render();
  }, [model, dark, selectedKeys, hover, showConcrete, showStirrups]);

  const pickAt = (clientX: number, clientY: number): KataWeb3dGroup | null => {
    const w = world.current;
    if (!w) return null;
    const rect = w.renderer.domElement.getBoundingClientRect();
    const ndc = new Vector2(((clientX - rect.left) / rect.width) * 2 - 1, -((clientY - rect.top) / rect.height) * 2 + 1);
    const ray = new Raycaster();
    ray.setFromCamera(ndc, w.rig.camera);
    const hit = ray.intersectObjects(w.bars.filter((m) => m.visible), false)[0];
    return hit ? (model.groups[hit.object.userData.index as number] ?? null) : null;
  };

  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    press.current = e.button === 0 ? { x: e.clientX, y: e.clientY } : null;
    e.currentTarget.focus();
  };
  const onPointerUp = (e: PointerEvent<HTMLDivElement>) => {
    const start = press.current;
    press.current = null;
    if (!start || e.button !== 0 || Math.hypot(e.clientX - start.x, e.clientY - start.y) > CLICK_PX) return;
    const group = pickAt(e.clientX, e.clientY);
    props.onSelectKeys(group && group.key !== '' ? [group.key] : null);
  };
  // The ViewCube menu is portalled to <body>, but React still bubbles its events here: ignore what is not ours.
  const fromOutside = (e: { target: EventTarget; currentTarget: HTMLDivElement }) => !e.currentTarget.contains(e.target as Node);
  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    if (e.buttons !== 0 || fromOutside(e)) return;
    const group = pickAt(e.clientX, e.clientY);
    const rect = e.currentTarget.getBoundingClientRect();
    setHover(group ? { group, x: e.clientX - rect.left, y: e.clientY - rect.top } : null);
  };
  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
    if (e.defaultPrevented || fromOutside(e)) return; // Esc that closed the menu is the menu's
    if (e.key === 'Delete' && selectedKeys && selectedKeys.length > 0) {
      props.removeBars(selectedKeys);
      props.onSelectKeys(null);
    } else if (e.key.toLowerCase() === 'z' && e.ctrlKey && props.canUndoRemoveBars) {
      props.undoRemoveBars();
    } else if (e.key === 'Escape') {
      if (selectedKeys) props.onSelectKeys(null);
      else props.onUnhandledEscape();
    } else return;
    e.preventDefault();
  };

  const rig = () => world.current?.rig;
  const t = useT();
  const instances = model.groups.reduce((sum, g) => sum + g.offsets.length, 0);
  const strip = `rounded border px-1.5 py-0.5 text-[11px] leading-4 ${dark ? 'border-white/20 bg-black/40 text-white' : 'border-black/15 bg-white/80 text-black'}`;
  const stop = { onPointerDown: (e: PointerEvent) => e.stopPropagation(), onPointerUp: (e: PointerEvent) => e.stopPropagation() };
  return (
    <div
      ref={box}
      tabIndex={0}
      className="relative h-full w-full overflow-hidden rounded outline-none"
      style={{ cursor: hover ? 'pointer' : 'default' }}
      data-testid="kata-3d"
      data-groups={model.groups.length}
      data-instances={instances}
      data-selected={selectedKeys?.length ?? 0}
      data-projection={projection}
      data-render-ms={renderMs.toFixed(1)}
      onPointerDown={onPointerDown}
      onPointerUp={onPointerUp}
      onPointerMove={onPointerMove}
      onPointerLeave={() => setHover(null)}
      onKeyDown={onKeyDown}
      onContextMenu={(e) => e.preventDefault()}
    >
      {failure ? (
        <div className="flex h-full items-center justify-center p-4 text-center text-[12px] text-muted" role="alert">
          {t.webgl3d} {failure}
        </div>
      ) : null}
      <div className="absolute top-2 left-2 flex flex-wrap gap-1" data-testid="kata-3d-tools" {...stop}>
        <label className={`${strip} inline-flex items-center gap-1`}>
          <input type="checkbox" checked={showConcrete} onChange={(e) => setShowConcrete(e.target.checked)} />
          {t.concrete}
        </label>
        <label className={`${strip} inline-flex items-center gap-1`}>
          <input type="checkbox" checked={showStirrups} onChange={(e) => setShowStirrups(e.target.checked)} />
          {t.stirrups}
        </label>
      </div>

      <div ref={cubeBox} className="absolute top-1 right-1 select-none" style={{ width: CUBE_PX, height: CUBE_PX }} {...stop}>
        <button
          type="button"
          aria-label={t.home}
          title={t.home}
          className="absolute top-0 left-0 z-10 grid size-6 place-items-center rounded text-muted hover:text-ink"
          onClick={() => world.current && world.current.rig.fit(world.current.home)}
        >
          <House className="size-4" />
        </button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button type="button" aria-label={t.cubeMenu} className="absolute right-0 bottom-0 z-10 grid size-6 place-items-center rounded text-muted hover:text-ink">
              <ChevronDown className="size-4" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56 text-[12.5px]">
            <DropdownMenuItem onSelect={() => world.current && world.current.rig.fit(world.current.home)}>{t.home}</DropdownMenuItem>
            <DropdownMenuItem onSelect={() => rig()?.fit()}>{t.fit}</DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuRadioGroup
              value={projection}
              onValueChange={(value) => {
                setProjection(value as Projection);
                rig()?.setProjection(value as Projection);
              }}
            >
              <DropdownMenuRadioItem value="orthographic">{t.orthographic}</DropdownMenuRadioItem>
              <DropdownMenuRadioItem value="perspective">{t.perspective}</DropdownMenuRadioItem>
            </DropdownMenuRadioGroup>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={() => world.current && (world.current.home = world.current.rig.direction())}>{t.setHome}</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className={`pointer-events-none absolute bottom-2 left-2 flex max-w-[calc(100%-1rem)] flex-wrap items-center gap-x-2 ${strip}`} data-testid="kata-3d-legend">
        {Object.entries(countRoles(model)).map(([role, count]) => (
          <span key={role} className="inline-flex items-center gap-1">
            <i className="inline-block h-2 w-2 rounded-full" style={{ background: `#${roleColor(role as KataBarRole, dark).toString(16).padStart(6, '0')}` }} />
            {t.roles3d[role as KataBarRole]} {count}
          </span>
        ))}
      </div>
      {hover ? (
        <div className={`pointer-events-none absolute ${strip}`} style={{ left: hover.x + 14, top: hover.y + 14 }} data-testid="kata-3d-tooltip">
          {describeGroup(hover.group, t)}
        </div>
      ) : null}
    </div>
  );
}

function disposeContent(w: World) {
  if (!w.content) return;
  w.scene.remove(w.content);
  w.content.traverse((o) => {
    if (o instanceof Mesh || o instanceof LineSegments || o instanceof InstancedMesh) {
      o.geometry.dispose();
      const material = o.material as { dispose(): void } | { dispose(): void }[];
      (Array.isArray(material) ? material : [material]).forEach((m) => m.dispose());
    }
  });
  w.content = null;
  w.concrete = null;
  w.bars = [];
}

function countRoles(model: KataWeb3dModel): Partial<Record<KataBarRole, number>> {
  const counts: Partial<Record<KataBarRole, number>> = {};
  for (const g of model.groups) counts[g.role] = (counts[g.role] ?? 0) + g.offsets.length;
  return counts;
}
