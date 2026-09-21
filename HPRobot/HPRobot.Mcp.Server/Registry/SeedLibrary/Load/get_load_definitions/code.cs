bool incRec = args.Bool("includeRecords", false);
var casesCol = structure.Cases.GetAll();
var simpleCases = new List<object>();
var combinations = new List<object>();

for (int i = 1; i <= casesCol.Count; i++)
{
    var c = casesCol.Get(i);
    if (c is IRobotSimpleCase sc)
    {
        var records = new List<object>();
        if (incRec)
        {
            for (int r = 1; r <= sc.Records.Count; r++)
            {
                var rec = sc.Records.Get(r);
                records.Add(new { index = r, type = rec.Type.ToString(), objects = rec.Objects.ToText() });
            }
        }
        simpleCases.Add(new
        {
            number = sc.Number,
            name = sc.Name,
            nature = sc.Nature.ToString(),
            recordCount = sc.Records.Count,
            records = incRec ? records : null
        });
    }
    else if (c is IRobotCaseCombination comb)
    {
        combinations.Add(new
        {
            number = comb.Number,
            name = comb.Name,
            type = comb.CombinationType.ToString(),
            caseComponents = comb.CaseFactors.Count
        });
    }
}

return new
{
    success = true,
    simpleCaseCount = simpleCases.Count,
    combinationCount = combinations.Count,
    simpleCases,
    combinations
};
