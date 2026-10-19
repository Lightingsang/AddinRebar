const fs = require('fs');
const path = require('path');
const { readManagedPeLayout } = require('C:\\Users\\STR-HP03\\AppData\\Roaming\\npm\\node_modules\\rea-agents\\dist\\dotnet\\ManagedPeReader.js');
const { readManagedMetadataLayout } = require('C:\\Users\\STR-HP03\\AppData\\Roaming\\npm\\node_modules\\rea-agents\\dist\\dotnet\\ManagedMetadataLayout.js');

const data = require('./cad_dlls_analysis.json');

const CAD_DLLS = [
  'Kata_pro64_Cad2025.dll',
  'Kata_pro64_Cad2013.dll',
  'Kata_pro64_GstarCad.dll',
  'Kata_pro64_GstarCad2025.dll',
  'Kata_pro64_ZwCad.dll'
];

function decodeSerString(blob, offset) {
  if (offset >= blob.length) return { str: null, nextOffset: offset };
  if (blob[offset] === 0xFF) return { str: null, nextOffset: offset + 1 };
  let len = blob[offset++];
  if ((len & 0x80) === 0) {
  } else if ((len & 0xC0) === 0x80) {
    len = ((len & 0x3F) << 8) | blob[offset++];
  } else if ((len & 0xE0) === 0xC0) {
    len = ((len & 0x1F) << 24) | (blob[offset++] << 16) | (blob[offset++] << 8) | blob[offset++];
  }
  const str = blob.slice(offset, offset + len).toString('utf8');
  return { str, nextOffset: offset + len };
}

const allResults = {};

for (const dll of CAD_DLLS) {
  const filePath = `C:\\kata_pro\\${dll}`;
  const bytes = fs.readFileSync(filePath);
  const pe = readManagedPeLayout(bytes);
  const mdOffset = pe.rvaToOffset(pe.cli.metadata.rva, pe.cli.metadata.size, 'cli.metadata');
  const metadata = readManagedMetadataLayout(bytes, mdOffset, pe.cli.metadata.size);

  function readBlob(blobIndex) {
    let cur = metadata.blob.offset + blobIndex;
    let len = bytes[cur++];
    if ((len & 0x80) === 0) {
    } else if ((len & 0xC0) === 0x80) {
      len = ((len & 0x3F) << 8) | bytes[cur++];
    } else if ((len & 0xE0) === 0xC0) {
      len = ((len & 0x1F) << 24) | (bytes[cur++] << 16) | (bytes[cur++] << 8) | bytes[cur++];
    }
    return bytes.slice(cur, cur + len);
  }

  const cadInfo = data[dll];
  const commands = [];

  for (const attr of cadInfo.command_attributes) {
    const parentSize = metadata.codedIndexSize('HasCustomAttribute');
    const typeSize = metadata.codedIndexSize('CustomAttributeType');
    const blobIdxSize = metadata.blobIndexSize;
    const off = attr.row_offset + parentSize + typeSize;
    const blobIdx = blobIdxSize === 4 ? bytes.readUInt32LE(off) : bytes.readUInt16LE(off);
    const blob = readBlob(blobIdx);
    
    // CustomAttribute blob prolog is 0x0001 (2 bytes)
    let cur = 2;
    const s1 = decodeSerString(blob, cur);
    cur = s1.nextOffset;
    const s2 = decodeSerString(blob, cur);
    
    // In AutoCAD CommandMethodAttribute:
    // constructor(string globalName) -> s1 = globalName
    // constructor(string groupName, string globalName) -> s1 = groupName, s2 = globalName
    let globalName = s1.str;
    let groupName = null;
    if (s2.str && s2.str.trim().length > 0) {
      groupName = s1.str;
      globalName = s2.str;
    }

    commands.push({
      command_name: globalName,
      group_name: groupName,
      raw_s1: s1.str,
      raw_s2: s2.str,
      parent_token: attr.parent_token,
      constructor_token: attr.constructor_token,
      token: attr.token,
      row_offset: attr.row_offset,
      value_length: attr.value_length,
      value_sha256: attr.value_sha256
    });
  }

  allResults[dll] = {
    commands_count: commands.length,
    commands,
    references: cadInfo.references,
    metadata_blob_offset: metadata.blob.offset,
    metadata_blob_size: metadata.blob.size
  };
}

fs.writeFileSync(
  path.join(__dirname, 'all_cad_commands_decoded.json'),
  JSON.stringify(allResults, null, 2),
  'utf8'
);

console.log('Decoded all commands successfully!');
for (const dll of CAD_DLLS) {
  console.log(`${dll}: ${allResults[dll].commands_count} commands. Blob offset: ${allResults[dll].metadata_blob_offset}`);
}
