int id = args.Int("objectId");
if (id <= 0) throw new ArgumentException("Valid objectId (> 0) is required.");
var props = args.Obj("properties");
if (props.IsEmpty) throw new ArgumentException("properties object cannot be empty.");

var mo = model.SelectModelObject(new Identifier(id));
if (mo == null) throw new ArgumentException($"ModelObject {id} not found in Tekla model.");

int count = 0;
foreach (var key in props.Keys)
{
    if (props.IntOrNull(key) is int intVal)
    {
        mo.SetUserProperty(key, intVal);
        count++;
    }
    else if (props.DoubleOrNull(key) is double dblVal)
    {
        mo.SetUserProperty(key, dblVal);
        count++;
    }
    else if (props.Str(key) is string strVal)
    {
        mo.SetUserProperty(key, strVal);
        count++;
    }
}

bool ok = mo.Modify();
if (!ok) throw new InvalidOperationException($"Failed to modify user properties on object {id}.");

log($"Modified {count} UDAs on object {id}");

return new
{
    success = true,
    objectId = id,
    modifiedCount = count
};
