# Gemini CLI Helper Script
# 使用方法: .\scripts\gemini-helper.ps1

param(
    [string]$Action = "help",
    [string]$File = "",
    [string]$Prompt = ""
)

function Show-Help {
    Write-Host "=== Gemini CLI Helper ===" -ForegroundColor Green
    Write-Host "使用方法:" -ForegroundColor Yellow
    Write-Host "  .\scripts\gemini-helper.ps1 -Action analyze -File src/app/components/GoogleMap.tsx"
    Write-Host "  .\scripts\gemini-helper.ps1 -Action suggest -Prompt 'お気に入り機能の実装方法'"
    Write-Host "  .\scripts\gemini-helper.ps1 -Action fix -File src/app/page.tsx"
    Write-Host ""
    Write-Host "利用可能なアクション:" -ForegroundColor Yellow
    Write-Host "  analyze  - ファイル分析"
    Write-Host "  suggest  - 機能提案"
    Write-Host "  fix      - バグ修正"
    Write-Host "  review   - コードレビュー"
    Write-Host "  help     - ヘルプ表示"
}

function Invoke-GeminiAnalyze {
    param([string]$File)
    $output = "gemini_analysis_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    Write-Host "分析中: $File" -ForegroundColor Blue
    gemini -a -p "Please analyze this file and suggest improvements. Focus on code quality, performance, and best practices." $File > $output
    Write-Host "結果を保存: $output" -ForegroundColor Green
    Get-Content $output
}

function Invoke-GeminiSuggest {
    param([string]$Prompt)
    $output = "gemini_suggestion_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    Write-Host "提案生成中..." -ForegroundColor Blue
    gemini -a -p $Prompt > $output
    Write-Host "結果を保存: $output" -ForegroundColor Green
    Get-Content $output
}

function Invoke-GeminiFix {
    param([string]$File)
    $output = "gemini_fix_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    Write-Host "修正提案生成中: $File" -ForegroundColor Blue
    gemini -a -p "Please identify and suggest fixes for any issues in this file. Provide specific code changes." $File > $output
    Write-Host "結果を保存: $output" -ForegroundColor Green
    Get-Content $output
}

function Invoke-GeminiReview {
    param([string]$File)
    $output = "gemini_review_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    Write-Host "コードレビュー中: $File" -ForegroundColor Blue
    gemini -a -p "Please review this code for best practices, potential issues, and improvement opportunities." $File > $output
    Write-Host "結果を保存: $output" -ForegroundColor Green
    Get-Content $output
}

# メイン処理
switch ($Action.ToLower()) {
    "analyze" { 
        if ($File) { Invoke-GeminiAnalyze -File $File }
        else { Write-Host "エラー: ファイル名を指定してください" -ForegroundColor Red }
    }
    "suggest" { 
        if ($Prompt) { Invoke-GeminiSuggest -Prompt $Prompt }
        else { Write-Host "エラー: プロンプトを指定してください" -ForegroundColor Red }
    }
    "fix" { 
        if ($File) { Invoke-GeminiFix -File $File }
        else { Write-Host "エラー: ファイル名を指定してください" -ForegroundColor Red }
    }
    "review" { 
        if ($File) { Invoke-GeminiReview -File $File }
        else { Write-Host "エラー: ファイル名を指定してください" -ForegroundColor Red }
    }
    default { Show-Help }
} 