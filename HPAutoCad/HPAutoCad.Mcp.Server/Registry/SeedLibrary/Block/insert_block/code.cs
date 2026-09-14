string blockName = args.Str("blockName");
if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName is required.");
var position = args.Obj("position");
double scale = args.Double("scale", 1);
if (scale <= 0) throw new ArgumentException("scale must be greater than 0.");
double rotation = args.Double("rotationDeg", 0) * Math.PI / 180.0;
string layer = args.Str("layer");
var attributes = args.Obj("attributes");
void RequireLayer(string layerName)
{
    var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
    if (!table.Has(layerName)) throw new ArgumentException($"Layer '{layerName}' does not exist in this drawing; run create_layer first or omit layer.");
}

var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
if (!blockTable.Has(blockName)) throw new ArgumentException($"Block '{blockName}' is not defined in this drawing; run list_block_definitions to see the available names.");
var definition = (BlockTableRecord)tr.GetObject(blockTable[blockName], OpenMode.ForRead);

var reference = new BlockReference(new Point3d(units.ToDrawing(position.Double("x")), units.ToDrawing(position.Double("y")), 0), definition.ObjectId)
{
    ScaleFactors = new Scale3d(scale),
    Rotation = rotation,
};
if (!string.IsNullOrWhiteSpace(layer)) { RequireLayer(layer); reference.Layer = layer; }

var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
space.AppendEntity(reference);
tr.AddNewlyCreatedDBObject(reference, true);

var attributesSet = 0;
var attributeTags = new List<string>();
foreach (ObjectId id in definition)
{
    if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition definitionAttribute || definitionAttribute.Constant) continue;

    var attribute = new AttributeReference();
    attribute.SetAttributeFromBlock(definitionAttribute, reference.BlockTransform);
    var value = attributes.Str(definitionAttribute.Tag);
    if (value != null) { attribute.TextString = value; attributesSet++; }
    reference.AttributeCollection.AppendAttribute(attribute);
    tr.AddNewlyCreatedDBObject(attribute, true);
    attributeTags.Add(definitionAttribute.Tag);
}

log($"block {blockName} inserted as {reference.Handle}, {attributesSet} attribute(s) set");
return new { handle = reference.Handle.ToString(), blockName, attributesSet, attributeTags };
