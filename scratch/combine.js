const fs = require('fs');
const tpl = fs.readFileSync('scratch/widget_template.html', 'utf8');
const data = fs.readFileSync('scratch/compact_sim_data.json', 'utf8');
const html = tpl.replace('__DATA_JSON__', data);

fs.writeFileSync('scratch/interactive_beam_simulation_widget.html', html, 'utf8');
const artPath = 'C:/Users/STR-HP03/.gemini/antigravity/brain/087c0df3-fff1-40c1-83ce-c0d4c6cb6acd/interactive_beam_simulation_widget.html';
fs.writeFileSync(artPath, html, 'utf8');
console.log('Widget created successfully! Size:', html.length);
