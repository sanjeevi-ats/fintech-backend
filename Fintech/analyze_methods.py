import openpyxl

wb = openpyxl.load_workbook('FinVeda_API_Test_Cases.xlsx')
for sheet_name in wb.sheetnames:
    ws = wb[sheet_name]
    for row_idx in range(2, min(50, ws.max_row + 1)):
        desc = str(ws.cell(row=row_idx, column=3).value or "").lower()
        endpoint = str(ws.cell(row=row_idx, column=2).value or "").lower()
        method = "UNKNOWN"
        if "delete " in desc:
            method = "DELETE"
        elif "update " in desc or "modify " in desc:
            method = "PUT"
        elif "create " in desc or "register " in desc or "login " in desc or "add " in desc or "assign " in desc:
            method = "POST"
        elif "get " in desc or "returns list " in desc or "returns data " in desc or "returns audit logs " in desc or "returns empty result " in desc or "returns 401 " in desc or "returns 403 " in desc or "returns 404 " in desc or "returns 400 " in desc:
            method = "GET"
            # It's possible some 'returns 400' are for POST, let's rely on endpoint keywords and context.
        print(f"Row {row_idx}: {method} - {endpoint} - {desc[:60]}")
