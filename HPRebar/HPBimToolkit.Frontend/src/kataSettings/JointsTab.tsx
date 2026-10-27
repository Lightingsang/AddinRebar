import { Info } from 'lucide-react';
import { NumberField, Section, SegmentedField, SwitchField, TextField, TextInput } from './fields';
import { useT } from './i18n';
import { sectionOf } from './navigation';
import type { KataJointForm, KataJointsForm, KataSettingsFormChoices } from './settingsContracts';

/** Kata's tab "Thép mặc định": the joint stirrups and hanger bars round a crossing beam and round a stub column. */
export function JointsTab({ joints, choices }: { joints: KataJointsForm; choices: KataSettingsFormChoices }) {
  const t = useT();
  return (
    <>
      <div className="grid gap-3 xl:grid-cols-2">
        <JointCard which="beam" id="sec-joint-beam" joint={joints.beam} choices={choices} />
        <JointCard which="column" id="sec-joint-column" joint={joints.column} choices={choices} />
      </div>
      <p className="flex gap-2.5 rounded-xl border border-primary/20 bg-primary/5 px-4 py-3 text-[12px] leading-relaxed text-muted-foreground">
        <Info className="mt-0.5 size-4 shrink-0 text-accent-strong" aria-hidden="true" />
        {t.jointsHelp}
      </p>
    </>
  );
}

function JointCard({ which, id, joint, choices }: { which: 'beam' | 'column'; id: string; joint: KataJointForm; choices: KataSettingsFormChoices }) {
  const t = useT();
  const p = (key: keyof KataJointForm) => `joints.${which}.${key}`;
  const off = !joint.hangerEnabled;
  return (
    <Section id={id} icon={sectionOf(id).icon} title={sectionOf(id).title(t)}>
      <TextField path={p('stirrups')} label={t.stirrups} placeholder="5f8a50" />
      <TextField path={p('stirrupCountSpec')} label={t.stirrupCountSpec} pending suggestions={choices.stirrupCountSpecs} />
      <SwitchField path={p('stirrupsThroughJoint')} label={t.stirrupsThroughJoint} pending />
      <SwitchField path={p('hangerEnabled')} label={t.hanger} errorPath={p('hanger')}>
        <TextInput path={p('hanger')} label={t.hanger} placeholder="2f16" disabled={off} />
      </SwitchField>
      <NumberField path={p('hangerTopLengthMm')} label={t.hangerTopLengthMm} unit="mm" disabled={off} />
      <SegmentedField path={p('hangerAngleDegrees')} label={t.hangerAngleDegrees} options={choices.hangerAngles.map((a) => ({ value: a, label: `${a}°` }))} />
    </Section>
  );
}
