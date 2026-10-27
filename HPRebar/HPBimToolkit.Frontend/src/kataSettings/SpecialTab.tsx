import { NumberField, Section, SwitchField } from './fields';
import { useT } from './i18n';
import { sectionOf } from './navigation';
import type { KataSpecialForm } from './settingsContracts';
import { ShopTable } from './ShopTable';

/** Kata's tab "Thông số đặc thù": laps and the two tables of Kata's dialog, all for the shop drawings. */
export function SpecialTab({ special }: { special: KataSpecialForm }) {
  const t = useT();
  const p = (key: keyof KataSpecialForm) => `special.${key}`;
  const section = (id: string) => ({ id, icon: sectionOf(id).icon, title: sectionOf(id).title(t) });
  return (
    <>
      <Section {...section('sec-lap-info')} pending note={t.specialNote}>
        <NumberField path={p('compressionLapFactor')} label={t.compressionLapFactor} unit="d" />
        <SwitchField path={p('allowTensionZoneLap')} label={t.allowTensionZoneLap} />
        <NumberField path={p('tensionLapFactor')} label={t.tensionLapFactor} unit="d" disabled={!special.allowTensionZoneLap} />
        <NumberField path={p('crankToLapEdgeMm')} label={t.crankToLapEdgeMm} unit="mm" />
        <NumberField path={p('minLapGapMm')} label={t.minLapGapMm} unit="mm" />
        <NumberField path={p('cuttingToleranceMm')} label={t.cuttingToleranceMm} unit="mm" />
      </Section>
      <Section {...section('sec-mass-table')} pending>
        <SwitchField path={p('useUnitMassTable')} label={t.useUnitMassTable} />
        <ShopTable
          path={p('massRows')}
          label={t.massTable}
          columns={[
            { key: 'diameter', label: t.diameter },
            { key: 'kgPerM', label: t.kgPerM },
          ]}
        />
      </Section>
      <Section {...section('sec-lap-table')} pending>
        <SwitchField path={p('useLapTable')} label={t.useLapTable} />
        <ShopTable
          path={p('lapRows')}
          label={t.lapTable}
          columns={[
            { key: 'diameter', label: t.diameter },
            { key: 'lapCompression', label: t.lapCompression },
            { key: 'lapTension', label: t.lapTension },
            { key: 'anchorCompression', label: t.anchorCompression },
            { key: 'anchorTension', label: t.anchorTension },
          ]}
        />
      </Section>
    </>
  );
}
