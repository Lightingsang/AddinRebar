import fs from 'node:fs';
import path from 'node:path';

const reaBase = 'C:/Users/STR-HP03/AppData/Roaming/npm/node_modules/rea-agents';
const { createEvidenceBundle } = await import(`file://${reaBase}/dist/domain/evidenceBundle.js`);

const dir = 'g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/.agents/teamwork/teamwork_preview_explorer_m1_1';

function readJsonFile(filename) {
    const filePath = path.join(dir, filename);
    const raw = fs.readFileSync(filePath);
    let str;
    if (raw[0] === 0xff && raw[1] === 0xfe) {
        str = raw.toString('utf16le');
    } else {
        str = raw.toString('utf8');
    }
    if (str.charCodeAt(0) === 0xfeff) {
        str = str.slice(1);
    }
    return JSON.parse(str.trim());
}

async function runFor(name) {
    const art = readJsonFile(`rea_artifact_${name}.json`);
    const mem = readJsonFile(`rea_members_${name}.json`);
    const nat = readJsonFile(`rea_native_${name}.json`);
    const graph = readJsonFile(`rea_graph_${name}.json`);

    const records = [art, mem, nat, graph];
    const bundle = createEvidenceBundle(records);

    const ledgerInput = {
        evidence_bundle: bundle,
        reviewed_obligations: [],
        manifest: {
            bindings: [],
            contradictions: []
        }
    };

    const outPath = path.join(dir, `ledger_input_${name}.json`);
    fs.writeFileSync(outPath, JSON.stringify(ledgerInput, null, 2), 'utf8');
    console.log(`Successfully generated ledger input for ${name} at ${outPath}`);
}

await runFor('Kata_Class_Lib');
await runFor('Kata_Class_Lib_Revit');
