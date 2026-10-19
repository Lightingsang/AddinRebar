// Quick script to generate test_model.json using HPBim.Core
using System;
using System.IO;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;

var model = NhaPho5x18mFixture.CreateModel();
var json = BimModelSerializer.Serialize(model);
File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "nhapho_5x18m_exported.json"), json);
Console.WriteLine("Exported nhapho_5x18m_exported.json successfully.");
