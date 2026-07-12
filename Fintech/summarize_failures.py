import openpyxl

def summarize():
    wb = openpyxl.load_workbook('FinVeda_Test_Execution_Report.xlsx')
    ws = wb.active
    
    print(f"{'ID':<10} | {'Status':<10} | {'Method':<10} | {'Endpoint':<50} | {'Error'}")
    print("-" * 100)
    
    for row in ws.iter_rows(min_row=2, values_only=True):
        test_id = row[0]
        actual_status = row[8]
        exec_status = row[10]
        exec_comments = row[11]
        endpoint = row[1]
        
        if exec_status == 'FAILED':
            print(f"{test_id:<10} | {actual_status:<10} | {exec_comments.split('Method inferred: ')[-1]:<10} | {endpoint:<50} | {exec_comments}")

if __name__ == "__main__":
    summarize()
