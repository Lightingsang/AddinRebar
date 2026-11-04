function Get-Rounded($v){ [math]::Round([double]$v,2) }
function Export-Doc($doc,[string]$Out,[string]$tag){
  $items=New-Object System.Collections.ArrayList
  foreach($e in $doc.ModelSpace){
    $o=[ordered]@{ h=$e.Handle; layer=$e.Layer; type=$e.ObjectName }
    switch -Wildcard ($e.ObjectName){
      'AcDbPolyline' { $c=$e.Coordinates; $pts=@(); for($i=0;$i -lt $c.Length;$i+=2){ $b=$e.GetBulge($i/2); $pts+= ,@((Get-Rounded $c[$i]),(Get-Rounded $c[$i+1]),[math]::Round($b,4)) }; $o.closed=$e.Closed; $o.pts=$pts }
      'AcDbLine' { $o.pts=@(@((Get-Rounded $e.StartPoint[0]),(Get-Rounded $e.StartPoint[1])),@((Get-Rounded $e.EndPoint[0]),(Get-Rounded $e.EndPoint[1]))) }
      'AcDbBlockReference' { $o.name=$e.EffectiveName; $o.at=@((Get-Rounded $e.InsertionPoint[0]),(Get-Rounded $e.InsertionPoint[1])); $o.scale=(Get-Rounded $e.XScaleFactor); $o.rot=(Get-Rounded ($e.Rotation*180/[math]::PI)); if($e.HasAttributes){ $a=[ordered]@{}; foreach($x in $e.GetAttributes()){ $a[$x.TagString]=$x.TextString }; $o.attrs=$a } }
      '*Dimension' { $o.value=(Get-Rounded $e.Measurement); $o.text=$e.TextOverride; $o.textAt=@((Get-Rounded $e.TextPosition[0]),(Get-Rounded $e.TextPosition[1])); try{ $o.rot=(Get-Rounded ($e.Rotation*180/[math]::PI)) }catch{} }
      'AcDbLeader' { $c=$e.Coordinates; $pts=@(); for($i=0;$i -lt $c.Length;$i+=3){ $pts+= ,@((Get-Rounded $c[$i]),(Get-Rounded $c[$i+1])) }; $o.pts=$pts }
      'AcDbText' { $o.text=$e.TextString; $o.at=@((Get-Rounded $e.InsertionPoint[0]),(Get-Rounded $e.InsertionPoint[1])); $o.height=(Get-Rounded $e.Height) }
    }
    [void]$items.Add($o)
  }
  $res=[ordered]@{ tag=$tag; count=$items.Count; entities=$items }
  [IO.File]::WriteAllText($Out,($res | ConvertTo-Json -Depth 8 -Compress),(New-Object Text.UTF8Encoding($false)))
  return $items.Count
}
