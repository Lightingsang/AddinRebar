import { HookField, LapZoneField, NumberField, NumberInput, RowNote, Section, SegmentedField, SelectField, SwitchField, type Option } from './fields';
import { useT } from './i18n';
import { sectionOf } from './navigation';
import type { KataDetailForm, KataSettingsFormChoices } from './settingsContracts';

const values = (list: number[]): Option[] => list.map((v) => ({ value: v, label: `${v}` }));

/** Kata's tab "Detail thép": what the bars are drawn with, the beam options kept for later and the shop settings. */
export function DetailTab({ detail, choices }: { detail: KataDetailForm; choices: KataSettingsFormChoices }) {
  const t = useT();
  const p = (key: keyof KataDetailForm) => `detail.${key}`;
  const section = (id: string) => ({ id, icon: sectionOf(id).icon, title: sectionOf(id).title(t) });
  return (
    <>
      <Section {...section('sec-bars')}>
        <SelectField path={p('crankMinDiameter')} label={t.crankMinDiameter} options={values(choices.crankMinDiameters)} unit="mm" />
        <SegmentedField path={p('crankSlope')} label={t.crankSlope} options={choices.crankSlopes.map((v) => ({ value: v, label: `1/${v}` }))} />
        <HookField label={t.closedStirrupHook} factorPath={p('closedStirrupHookFactor')} anglePath={p('closedStirrupHookAngle')} angles={choices.hookAngles} />
        <HookField label={t.crossTieHook} factorPath={p('crossTieHookFactor')} anglePath={p('crossTieHookAngle')} angles={choices.hookAngles} />
        <NumberField path={p('roundLegMm')} label={t.roundLegMm} unit="mm" />
        <NumberField path={p('roundCutExtraMm')} label={t.roundCutExtraMm} unit="mm" />
      </Section>
      <Section {...section('sec-zones')}>
        <NumberField path={p('curtailedExtensionMm')} label={t.curtailedExtensionMm} unit="mm" />
        <NumberField path={p('bottomExtraCutFraction')} label={t.bottomExtraCutFraction} unit="ln" />
        <NumberField path={p('denseZoneHeightFactor')} label={t.denseZoneHeightFactor} unit="h" />
        <NumberField path={p('endZoneFraction')} label={t.endZoneFraction} unit="ln" />
        <NumberField path={p('layerClearGap')} label={t.layerClearGap} unit="mm" />
      </Section>
      <Section {...section('sec-side-bars')}>
        <NumberField path={p('sideBarAnchorageFactor')} label={t.sideBarAnchorageFactor} unit="d" />
        <NumberField path={p('layerTieMinBarCount')} label={t.layerTieMinBarCount} unit="bars" integer />
        <NumberField path={p('sideBarRequiredHeight')} label={t.sideBarRequiredHeight} unit="mm" />
        <NumberField path={p('minimumLegFactor')} label={t.minimumLegFactor} unit="d" />
      </Section>
      <Section {...section('sec-drawing')}>
        <SwitchField path={p('showBarEndMarks')} label={t.showBarEndMarks} />
        <SwitchField path={p('createKataDrawings')} label={t.createKataDrawings} />
        <RowNote>{t.pendingNote}</RowNote>
        <SwitchField path={p('bottomLayerNoBend')} label={t.bottomLayerNoBend} pending />
        <SwitchField path={p('topNotAnchoredIntoColumnBelow')} label={t.topNotAnchoredIntoColumnBelow} pending />
        <SwitchField path={p('alwaysBend')} label={t.alwaysBend} pending errorPath={p('alwaysBendFactor')}>
          <NumberInput path={p('alwaysBendFactor')} unit="d" label={`${t.alwaysBend} (d)`} disabled={!detail.alwaysBend} />
        </SwitchField>
        <SwitchField path={p('cutContinuousBarsAtSpanEnds')} label={t.cutContinuousBarsAtSpanEnds} pending />
        <SwitchField path={p('differentNumbersForIdenticalBars')} label={t.differentNumbersForIdenticalBars} pending />
      </Section>
      <Section {...section('sec-shop')} pending note={t.shopNote}>
        <SelectField path={p('couplerMinDiameter')} label={t.couplerMinDiameter} options={values(choices.couplerMinDiameters)} unit="mm" />
        <NumberField path={p('maxBarLengthMm')} label={t.maxBarLengthMm} unit="mm" />
        <NumberField path={p('minLengthForCuttingMm')} label={t.minLengthForCuttingMm} unit="mm" />
        <NumberField path={p('minBarLengthFactor')} label={t.minBarLengthFactor} unit="d" />
        <NumberField path={p('roundLapMm')} label={t.roundLapMm} unit="mm" />
      </Section>
      <Section {...section('sec-laps')} pending>
        <SwitchField path={p('topLapAtMidspan')} label={t.topLapAtMidspan} />
        <SwitchField path={p('topLapAtSupport')} label={t.topLapAtSupport} />
        <SwitchField path={p('bottomLapAtSupport')} label={t.bottomLapAtSupport} />
        <SwitchField path={p('bottomLapAtMidspan')} label={t.bottomLapAtMidspan} />
        <LapZoneField label={t.topLapZone} fractionPath={p('topLapZoneFraction')} originPath={p('topLapZoneFromCentre')} />
        <LapZoneField label={t.bottomLapZone} fractionPath={p('bottomLapZoneFraction')} originPath={p('bottomLapZoneFromCentre')} />
        <SwitchField path={p('preferFewerLaps')} label={t.preferFewerLaps} />
      </Section>
    </>
  );
}
