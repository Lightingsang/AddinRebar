// The 3D view's cameras, moved as Revit moves its 3D view: orthographic by default (Revit's {3D}) or perspective,
// fit to view, animated turns to a ViewCube direction, orbit about a pivot (the selection or the model) with Revit's
// green pivot ball while it turns. Pan and zoom to the cursor are OrbitControls' own (middle button, wheel).
import { Mesh, MeshBasicMaterial, MOUSE, OrthographicCamera, PerspectiveCamera, Quaternion, Scene, SphereGeometry, Vector3, type Camera } from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import type { Bounds3 } from './model3dGeometry';
import { fitCamera, orbitAbout, regionDirection, type CubeRegion } from './revitNavigation';

export const FOV = 35;
const TURN_MS = 350;
const PIVOT_SCREEN_SHARE = 0.012;

export type Projection = 'orthographic' | 'perspective';

export class CameraRig {
  readonly perspective = new PerspectiveCamera(FOV, 1, 10, 1e6);
  readonly orthographic = new OrthographicCamera(-1, 1, 1, -1, 0.1, 1e7);
  readonly controls: OrbitControls;
  private readonly pivotBall = new Mesh(new SphereGeometry(1, 16, 12), new MeshBasicMaterial({ color: 0x2ecc40, depthTest: false }));
  private aspect = 1;
  /** Half the height the orthographic camera shows at zoom 1. */
  private orthoHalf = 1000;
  private bounds: Bounds3 = { min: { x: 0, y: 0, z: 0 }, max: { x: 1, y: 1, z: 1 } };
  private turning = 0;
  projection: Projection = 'orthographic';

  constructor(
    element: HTMLElement,
    scene: Scene,
    private readonly render: () => void,
  ) {
    for (const camera of [this.perspective, this.orthographic]) camera.up.set(0, 0, 1);
    this.controls = new OrbitControls(this.orthographic, element);
    // Revit: the wheel zooms about the cursor, the middle button pans, Shift + middle orbits (handled by the view, about
    // a pivot), the left button selects, the right button does nothing here.
    this.controls.mouseButtons = { LEFT: null, MIDDLE: MOUSE.PAN, RIGHT: null };
    this.controls.enableRotate = false;
    this.controls.zoomToCursor = true;
    this.controls.screenSpacePanning = true;
    this.controls.addEventListener('change', render);
    this.pivotBall.visible = false;
    this.pivotBall.renderOrder = 10;
    scene.add(this.pivotBall);
  }

  get camera(): PerspectiveCamera | OrthographicCamera {
    return this.projection === 'orthographic' ? this.orthographic : this.perspective;
  }

  setBounds(bounds: Bounds3): void {
    this.bounds = bounds;
  }

  setAspect(aspect: number): void {
    this.aspect = aspect;
    this.perspective.aspect = aspect;
    this.perspective.updateProjectionMatrix();
    this.updateOrthoFrustum();
  }

  /** Where the camera looks from (unit vector from the target toward the camera). */
  direction(): Vector3 {
    return this.camera.position.clone().sub(this.controls.target).normalize();
  }

  /** Fits the model, looking from `direction` (the current one when left out); animated unless `instant`. */
  fit(direction = this.direction(), instant = false): void {
    const fit = fitCamera(direction, this.bounds, FOV, this.aspect);
    const end = { position: new Vector3(fit.position.x, fit.position.y, fit.position.z), target: new Vector3(fit.target.x, fit.target.y, fit.target.z), half: fit.orthoHalfHeight };
    if (instant) this.place(end.target, end.position.clone().sub(end.target).normalize(), end.position.distanceTo(end.target), end.half);
    else this.turnTo(end);
  }

  goTo(region: CubeRegion): void {
    this.fit(regionDirection(region));
  }

  /** Revit's Shift + middle drag (or a drag on the ViewCube): about the pivot, with the green ball shown. */
  orbit(azimuth: number, elevation: number, pivot: Vector3): void {
    cancelAnimationFrame(this.turning);
    const r = orbitAbout(this.camera.position, this.controls.target, pivot, azimuth, elevation);
    this.camera.position.copy(r.position);
    this.controls.target.copy(r.target);
    this.showPivot(pivot);
    this.controls.update();
  }

  hidePivot(): void {
    this.pivotBall.visible = false;
    this.render();
  }

  /** The model's centre: what the view orbits about when nothing is selected. */
  modelCentre(): Vector3 {
    const { min, max } = this.bounds;
    return new Vector3((min.x + max.x) / 2, (min.y + max.y) / 2, (min.z + max.z) / 2);
  }

  /** Switches between Revit's orthographic and perspective 3D, keeping what is on screen the same size. */
  setProjection(projection: Projection): void {
    if (projection === this.projection) return;
    const target = this.controls.target.clone();
    const dir = this.direction();
    const tan = Math.tan(((FOV / 2) * Math.PI) / 180);
    const half = this.visibleHalfHeight();
    this.projection = projection;
    this.controls.object = this.camera;
    this.place(target, dir, half / tan, half);
  }

  /** Half the height of the model space on screen, at the target. */
  visibleHalfHeight(): number {
    if (this.projection === 'orthographic') return this.orthoHalf / this.orthographic.zoom;
    return this.perspective.position.distanceTo(this.controls.target) * Math.tan(((FOV / 2) * Math.PI) / 180);
  }

  dispose(): void {
    cancelAnimationFrame(this.turning);
    this.controls.dispose();
    this.pivotBall.geometry.dispose();
    (this.pivotBall.material as MeshBasicMaterial).dispose();
  }

  /** Puts the camera `distance` from `target` along `dir` (perspective) or far outside the model (orthographic). */
  private place(target: Vector3, dir: Vector3, distance: number, half: number): void {
    const { min, max } = this.bounds;
    const radius = Math.hypot(max.x - min.x, max.y - min.y, max.z - min.z) / 2;
    this.controls.target.copy(target);
    this.orthoHalf = half;
    this.orthographic.zoom = 1;
    this.updateOrthoFrustum();
    const camera: Camera = this.camera;
    const away = this.projection === 'orthographic' ? Math.max(distance, radius * 3 + 1000) : distance;
    camera.position.copy(target).addScaledVector(dir, away);
    this.perspective.far = Math.max(1e5, away * 4);
    this.perspective.updateProjectionMatrix();
    this.controls.update();
  }

  /** Animates direction, target and size to `end` (Revit turns the view rather than jumping). */
  private turnTo(end: { position: Vector3; target: Vector3; half: number }): void {
    cancelAnimationFrame(this.turning);
    const startTarget = this.controls.target.clone();
    const startDir = this.direction();
    const endDir = end.position.clone().sub(end.target).normalize();
    const startDistance = this.camera.position.distanceTo(startTarget);
    const endDistance = end.position.distanceTo(end.target);
    const startHalf = this.visibleHalfHeight();
    const turn = new Quaternion().setFromUnitVectors(startDir, endDir);
    const started = performance.now();
    const step = () => {
      const t = Math.min(1, (performance.now() - started) / TURN_MS);
      const e = t < 0.5 ? 2 * t * t : 1 - (-2 * t + 2) ** 2 / 2;
      const dir = startDir.clone().applyQuaternion(new Quaternion().slerp(turn, e)).normalize();
      const target = startTarget.clone().lerp(end.target, e);
      this.place(target, dir, startDistance + (endDistance - startDistance) * e, startHalf + (end.half - startHalf) * e);
      if (t < 1) this.turning = requestAnimationFrame(step);
    };
    step();
  }

  private updateOrthoFrustum(): void {
    const o = this.orthographic;
    o.top = this.orthoHalf;
    o.bottom = -this.orthoHalf;
    o.left = -this.orthoHalf * this.aspect;
    o.right = this.orthoHalf * this.aspect;
    o.updateProjectionMatrix();
  }

  private showPivot(pivot: Vector3): void {
    this.pivotBall.position.copy(pivot);
    this.pivotBall.scale.setScalar(this.visibleHalfHeight() * 2 * PIVOT_SCREEN_SHARE);
    this.pivotBall.visible = true;
  }
}
