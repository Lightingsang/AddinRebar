// The 3D view of the plan (three.js, its own chunk): bars as tubes instanced per shape, the concrete translucent,
// orbit / pan / zoom with the mouse, view presets. Renders on demand (a change of view, size, selection or theme),
// never in a loop, so Revit's own window keeps the GPU. A click selects a bar group by the key the elevation uses:
// Delete removes it, Ctrl+Z undoes, Esc clears it, then closes the window as the elevation does.
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
  PerspectiveCamera,
  Raycaster,
  Scene,
  Vector2,
  WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import type { KataTheme } from '../kataContracts';
import type { KataBarRole, KataWeb3dGroup, KataWeb3dModel } from '../kataScene';
import {
  HOVER_COLOR,
  ROLE_NAMES,
  SELECTED_COLOR,
  VIEW_PRESETS,
  barGeometry,
  boxGeometry,
  describeGroup,
  isStirrup,
  modelBounds,
  presetCamera,
  roleColor,
  type ViewPreset,
} from './model3dGeometry';

const FOV = 35;
const CLICK_PX = 4;

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
  camera: PerspectiveCamera;
  controls: OrbitControls;
  render(): void;
  content: Group | null;
  bars: InstancedMesh[];
  concrete: Group | null;
}

export default function Rebar3DView(props: Rebar3DViewProps) {
  const { model, theme, selectedKeys } = props;
  const box = useRef<HTMLDivElement>(null);
  const world = useRef<World | null>(null);
  const [failure, setFailure] = useState<string | null>(null);
  const [showConcrete, setShowConcrete] = useState(true);
  const [showStirrups, setShowStirrups] = useState(true);
  const [hover, setHover] = useState<{ group: KataWeb3dGroup; x: number; y: number } | null>(null);
  const [renderMs, setRenderMs] = useState(0);
  const press = useRef<{ x: number; y: number } | null>(null);
  const dark = theme === 'dark';

  // The renderer, camera and controls live as long as the view.
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
    const camera = new PerspectiveCamera(FOV, 1, 10, 1e6);
    camera.up.set(0, 0, 1);
    const controls = new OrbitControls(camera, renderer.domElement);
    controls.screenSpacePanning = true;

    const w: World = {
      renderer,
      scene,
      camera,
      controls,
      content: null,
      bars: [],
      concrete: null,
      render: () => {
        const started = performance.now();
        renderer.render(scene, camera);
        setRenderMs(performance.now() - started);
      },
    };
    world.current = w;
    controls.addEventListener('change', w.render);

    const resize = () => {
      const width = Math.max(1, element.clientWidth);
      const height = Math.max(1, element.clientHeight);
      renderer.setSize(width, height);
      camera.aspect = width / height;
      camera.updateProjectionMatrix();
      w.render();
    };
    const observer = new ResizeObserver(resize);
    observer.observe(element);
    resize();

    return () => {
      observer.disconnect();
      controls.dispose();
      disposeContent(w);
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

    if (first) frame(w, model, 'iso');
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
    ray.setFromCamera(ndc, w.camera);
    const hit = ray.intersectObjects(w.bars.filter((m) => m.visible), false)[0];
    return hit ? (model.groups[hit.object.userData.index as number] ?? null) : null;
  };

  const onPointerDown = (e: PointerEvent<HTMLDivElement>) => {
    press.current = { x: e.clientX, y: e.clientY };
    e.currentTarget.focus();
  };
  const onPointerUp = (e: PointerEvent<HTMLDivElement>) => {
    const start = press.current;
    press.current = null;
    if (!start || e.button !== 0 || Math.hypot(e.clientX - start.x, e.clientY - start.y) > CLICK_PX) return;
    const group = pickAt(e.clientX, e.clientY);
    props.onSelectKeys(group && group.key !== '' ? [group.key] : null);
  };
  const onPointerMove = (e: PointerEvent<HTMLDivElement>) => {
    if (e.buttons !== 0) return;
    const group = pickAt(e.clientX, e.clientY);
    const rect = e.currentTarget.getBoundingClientRect();
    setHover(group ? { group, x: e.clientX - rect.left, y: e.clientY - rect.top } : null);
  };
  const onKeyDown = (e: KeyboardEvent<HTMLDivElement>) => {
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

  const instances = model.groups.reduce((sum, g) => sum + g.offsets.length, 0);
  const strip = `rounded border px-1.5 py-0.5 text-[11px] leading-4 ${dark ? 'border-white/20 bg-black/40 text-white' : 'border-black/15 bg-white/80 text-black'}`;
  return (
    <div
      ref={box}
      tabIndex={0}
      className="relative h-full w-full overflow-hidden rounded outline-none"
      data-testid="kata-3d"
      data-groups={model.groups.length}
      data-instances={instances}
      data-selected={selectedKeys?.length ?? 0}
      data-render-ms={renderMs.toFixed(1)}
      onPointerDown={onPointerDown}
      onPointerUp={onPointerUp}
      onPointerMove={onPointerMove}
      onPointerLeave={() => setHover(null)}
      onKeyDown={onKeyDown}
    >
      {failure ? (
        <div className="flex h-full items-center justify-center p-4 text-center text-[12px] text-muted" role="alert">
          Không mở được hình 3D (WebGL): {failure}
        </div>
      ) : null}
      <div className="absolute left-2 top-2 flex flex-wrap gap-1" data-testid="kata-3d-tools" onPointerDown={(e) => e.stopPropagation()} onPointerUp={(e) => e.stopPropagation()}>
        {VIEW_PRESETS.map((p) => (
          <button key={p.id} type="button" className={strip} onClick={() => world.current && (frame(world.current, model, p.id), world.current.render())}>
            {p.label}
          </button>
        ))}
        <label className={`${strip} inline-flex items-center gap-1`}>
          <input type="checkbox" checked={showConcrete} onChange={(e) => setShowConcrete(e.target.checked)} />
          Bê tông
        </label>
        <label className={`${strip} inline-flex items-center gap-1`}>
          <input type="checkbox" checked={showStirrups} onChange={(e) => setShowStirrups(e.target.checked)} />
          Đai
        </label>
      </div>
      <div className={`pointer-events-none absolute bottom-2 left-2 flex flex-wrap items-center gap-x-2 ${strip}`} data-testid="kata-3d-count">
        <b>
          {instances}/{model.barCount} thanh
        </b>
        {Object.entries(countRoles(model)).map(([role, count]) => (
          <span key={role} className="inline-flex items-center gap-1">
            <i className="inline-block h-2 w-2 rounded-full" style={{ background: `#${roleColor(role as KataBarRole, dark).toString(16).padStart(6, '0')}` }} />
            {ROLE_NAMES[role as KataBarRole]} {count}
          </span>
        ))}
      </div>
      {hover ? (
        <div className={`pointer-events-none absolute ${strip}`} style={{ left: hover.x + 14, top: hover.y + 14 }} data-testid="kata-3d-tooltip">
          {describeGroup(hover.group)}
        </div>
      ) : null}
    </div>
  );
}

function frame(w: World, model: KataWeb3dModel, preset: ViewPreset) {
  const { position, target } = presetCamera(preset, modelBounds(model), FOV, w.camera.aspect);
  w.camera.position.set(position.x, position.y, position.z);
  w.controls.target.set(target.x, target.y, target.z);
  w.camera.far = Math.max(1e5, Math.hypot(position.x - target.x, position.y - target.y, position.z - target.z) * 4);
  w.camera.updateProjectionMatrix();
  w.controls.update();
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
