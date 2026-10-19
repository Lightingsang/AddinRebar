import json

with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/block_attributes.txt', 'r', encoding='latin1') as f:
    lines = f.readlines()

blocks = {}
current_blk = None
for line in lines:
    line = line.strip()
    if line.startswith('BLOCK: '):
        current_blk = line.replace('BLOCK: ', '')
        blocks[current_blk] = []
    elif line.startswith('ATTDEF: ') and current_blk:
        # Format: ATTDEF: Tag="001" Prompt="SH" Default="1" Height=250.0 Style="kata_text" Layer="kata_text" Flags=0
        rest = line[len('ATTDEF: '):]
        attr = {}
        # Simple parser for Key=Val
        import re
        tokens = re.findall(r'(\w+)=(?:"([^"]*)"|(\S+))', rest)
        for k, v1, v2 in tokens:
            attr[k] = v1 if v1 != '' else v2
        blocks[current_blk].append(attr)

filtered = {k: v for k, v in blocks.items() if not k.startswith('*D') and not k.startswith('*U')}

with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/dwg_blocks_inventory.json', 'w', encoding='utf-8') as out:
    json.dump(filtered, out, indent=2, ensure_ascii=False)

print(f"Total non-anonymous blocks: {len(filtered)}")
with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/block_summary.txt', 'w', encoding='utf-8') as s:
    for k, v in sorted(filtered.items()):
        tags = [a.get('Tag', '') for a in v]
        prompts = [a.get('Prompt', '') for a in v]
        defaults = [a.get('Default', '') for a in v]
        s.write(f"=== {k} ({len(v)} attrs) ===\n")
        for a in v:
            s.write(f"  Tag: {a.get('Tag',''):<15} Prompt: {a.get('Prompt',''):<25} Default: {a.get('Default',''):<15} Height: {a.get('Height',''):<8} Layer: {a.get('Layer',''):<15}\n")
        s.write("\n")
