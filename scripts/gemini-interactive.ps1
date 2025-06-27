# Gemini Interactive Development Script
# 対話的な開発フローを提供

function Start-GeminiInteractive {
    Write-Host "=== Gemini Interactive Development ===" -ForegroundColor Green
    Write-Host "Smokers Oasis Map プロジェクト開発支援" -ForegroundColor Yellow
    Write-Host ""
    
    while ($true) {
        Write-Host "選択してください:" -ForegroundColor Cyan
        Write-Host "1. ファイル分析・改善提案" -ForegroundColor White
        Write-Host "2. 新機能実装相談" -ForegroundColor White
        Write-Host "3. バグ修正・デバッグ支援" -ForegroundColor White
        Write-Host "4. コードレビュー" -ForegroundColor White
        Write-Host "5. パフォーマンス最適化" -ForegroundColor White
        Write-Host "6. 直接Geminiに質問" -ForegroundColor White
        Write-Host "0. 終了" -ForegroundColor Red
        Write-Host ""
        
        $choice = Read-Host "選択 (0-6)"
        
        switch ($choice) {
            "1" { Invoke-FileAnalysis }
            "2" { Invoke-FeatureConsultation }
            "3" { Invoke-BugFixSupport }
            "4" { Invoke-CodeReview }
            "5" { Invoke-PerformanceOptimization }
            "6" { Invoke-DirectQuestion }
            "0" { 
                Write-Host "開発支援を終了します。" -ForegroundColor Green
                return 
            }
            default { Write-Host "無効な選択です。" -ForegroundColor Red }
        }
        
        Write-Host ""
        Read-Host "Enterキーを押して続行"
        Clear-Host
    }
}

function Invoke-FileAnalysis {
    Write-Host "=== ファイル分析・改善提案 ===" -ForegroundColor Blue
    $file = Read-Host "分析したいファイルのパスを入力 (例: src/app/components/GoogleMap.tsx)"
    
    if (Test-Path $file) {
        $output = "analysis_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        Write-Host "分析中..." -ForegroundColor Yellow
        gemini -a -p "Analyze this file and suggest specific improvements for code quality, performance, and best practices. Focus on actionable recommendations." $file > $output
        Write-Host "分析完了: $output" -ForegroundColor Green
        Get-Content $output
    } else {
        Write-Host "ファイルが見つかりません: $file" -ForegroundColor Red
    }
}

function Invoke-FeatureConsultation {
    Write-Host "=== 新機能実装相談 ===" -ForegroundColor Blue
    Write-Host "実装したい機能を説明してください:" -ForegroundColor Yellow
    $feature = Read-Host "機能説明"
    
    $output = "feature_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    Write-Host "実装提案生成中..." -ForegroundColor Yellow
    gemini -a -p "For the Smokers Oasis Map project, suggest implementation approach for: $feature. Include file structure, API endpoints, database changes, and UI components needed." > $output
    Write-Host "提案完了: $output" -ForegroundColor Green
    Get-Content $output
}

function Invoke-BugFixSupport {
    Write-Host "=== バグ修正・デバッグ支援 ===" -ForegroundColor Blue
    $file = Read-Host "問題のあるファイルのパスを入力"
    $issue = Read-Host "問題の詳細を説明してください"
    
    if (Test-Path $file) {
        $output = "bugfix_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        Write-Host "修正提案生成中..." -ForegroundColor Yellow
        gemini -a -p "Analyze this file and suggest fixes for: $issue. Provide specific code changes and explain the root cause." $file > $output
        Write-Host "修正提案完了: $output" -ForegroundColor Green
        Get-Content $output
    } else {
        Write-Host "ファイルが見つかりません: $file" -ForegroundColor Red
    }
}

function Invoke-CodeReview {
    Write-Host "=== コードレビュー ===" -ForegroundColor Blue
    $file = Read-Host "レビューしたいファイルのパスを入力"
    
    if (Test-Path $file) {
        $output = "review_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        Write-Host "レビュー中..." -ForegroundColor Yellow
        gemini -a -p "Conduct a comprehensive code review for this file. Check for best practices, potential issues, security concerns, and improvement opportunities." $file > $output
        Write-Host "レビュー完了: $output" -ForegroundColor Green
        Get-Content $output
    } else {
        Write-Host "ファイルが見つかりません: $file" -ForegroundColor Red
    }
}

function Invoke-PerformanceOptimization {
    Write-Host "=== パフォーマンス最適化 ===" -ForegroundColor Blue
    $file = Read-Host "最適化したいファイルのパスを入力"
    
    if (Test-Path $file) {
        $output = "performance_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        Write-Host "最適化提案生成中..." -ForegroundColor Yellow
        gemini -a -p "Analyze this file for performance optimization opportunities. Focus on React rendering, data fetching, memory usage, and bundle size." $file > $output
        Write-Host "最適化提案完了: $output" -ForegroundColor Green
        Get-Content $output
    } else {
        Write-Host "ファイルが見つかりません: $file" -ForegroundColor Red
    }
}

function Invoke-DirectQuestion {
    Write-Host "=== 直接質問 ===" -ForegroundColor Blue
    $question = Read-Host "Geminiに質問したい内容を入力してください"
    
    $output = "question_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    Write-Host "回答生成中..." -ForegroundColor Yellow
    gemini -a -p $question > $output
    Write-Host "回答完了: $output" -ForegroundColor Green
    Get-Content $output
}

# スクリプト実行
Start-GeminiInteractive 