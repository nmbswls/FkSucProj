import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";
import fs from "node:fs/promises";

const input = await FileBlob.load("C:/UProjects/FkSucProj/FkSucProj/Config/Datas/cult_tech.xlsx");
const workbook = await SpreadsheetFile.importXlsx(input);
const summary = await workbook.inspect({
  kind: "workbook,sheet,table,region",
  maxChars: 12000,
  tableMaxRows: 20,
  tableMaxCols: 20,
  tableMaxCellChars: 120,
});
await fs.writeFile("C:/UProjects/FkSucProj/FkSucProj/.codex-tmp/cult_workbook_inspect.txt", summary.ndjson ?? String(summary), "utf8");
const sheets = await workbook.inspect({ kind: "sheet", include: "id,name", maxChars: 4000 });
await fs.writeFile("C:/UProjects/FkSucProj/FkSucProj/.codex-tmp/cult_workbook_sheets.txt", sheets.ndjson ?? String(sheets), "utf8");
for (const sheet of workbook.worksheets.items) {
  const preview = await workbook.render({ sheetName: sheet.name, autoCrop: "all", scale: 1, format: "png" });
  await fs.writeFile(`C:/UProjects/FkSucProj/FkSucProj/.codex-tmp/${sheet.name}.png`, new Uint8Array(await preview.arrayBuffer()));
}
