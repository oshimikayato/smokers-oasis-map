# Smokers Oasis Map 🚬🗺️

喫煙所・喫煙可能店舗のマップアプリケーションです。Google Maps APIを使用して、喫煙所や喫煙可能な飲食店を地図上で検索・登録・管理できます。

## 🌟 主な機能

- **🗺️ インタラクティブマップ**: Google Mapsを使用した直感的な地図表示
- **🔍 高度な検索**: キーワード、カテゴリ、タグによる絞り込み検索
- **📍 距離順ソート**: 現在地からの距離で近い順にソート
- **📸 写真ギャラリー**: 各スポットの写真投稿・閲覧機能
- **💬 フィードバックシステム**: スポットの存在確認・評価・コメント
- **⭐ お気に入り機能**: よく使うスポットをお気に入り登録
- **📊 統計情報**: スポット数、写真数、フィードバック数の統計表示
- **🚨 通報機能**: 閉鎖・移転などの情報を通報

## 🚀 デプロイ方法

### Vercelでのデプロイ（推奨）

1. **GitHubにプッシュ**
   ```bash
   git add .
   git commit -m "Initial commit"
   git push origin main
   ```

2. **Vercelでデプロイ**
   - [Vercel](https://vercel.com)にアクセス
   - GitHubアカウントでログイン
   - "New Project" → GitHubリポジトリを選択
   - 環境変数を設定（後述）
   - "Deploy"をクリック

3. **環境変数の設定**
   Vercelのダッシュボードで以下を設定：
   ```
   NEXT_PUBLIC_GOOGLE_MAPS_API_KEY=your_google_maps_api_key
   DATABASE_URL=your_postgresql_database_url
   ```

### その他のデプロイ方法

- **Netlify**: 静的サイトとしてデプロイ可能
- **Railway**: フルスタックアプリケーションとしてデプロイ可能
- **AWS/GCP**: カスタムサーバーでのデプロイ

## 🛠️ 開発環境のセットアップ

### 必要なもの
- Node.js 18以上
- PostgreSQL（本番環境）
- Google Maps API Key

### インストール
```bash
# 依存関係のインストール
npm install

# 環境変数の設定
cp .env.example .env.local
# .env.localを編集してAPIキーを設定

# データベースのセットアップ
npx prisma migrate dev
npx prisma generate

# 開発サーバーの起動
npm run dev
```

### 環境変数
```bash
# .env.local
NEXT_PUBLIC_GOOGLE_MAPS_API_KEY=your_google_maps_api_key
DATABASE_URL=your_database_url
```

## 📱 技術スタック

- **フロントエンド**: Next.js 15, React, TypeScript
- **スタイリング**: Tailwind CSS
- **データベース**: PostgreSQL + Prisma
- **地図**: Google Maps JavaScript API
- **デプロイ**: Vercel（推奨）

## 🎨 UI/UX 特徴

- **モダンデザイン**: 美しいグラデーションとアニメーション
- **レスポンシブ対応**: スマートフォン・タブレット・PC対応
- **直感的な操作**: ドラッグ&ドロップ、ワンクリック操作
- **アクセシビリティ**: キーボードナビゲーション対応

## 🔧 カスタマイズ

### カラーテーマの変更
`src/app/globals.css`のCSS変数を編集：
```css
:root {
  --primary: #3b82f6;
  --secondary: #10b981;
  --accent: #f59e0b;
  /* 他の色も変更可能 */
}
```

### 新しい機能の追加
- 新しいAPIエンドポイント: `src/app/api/`
- 新しいコンポーネント: `src/app/components/`
- 新しいページ: `src/app/`

## 📊 パフォーマンス最適化

- **画像最適化**: Next.js Imageコンポーネント使用
- **コード分割**: 動的インポートでバンドルサイズ削減
- **キャッシュ戦略**: APIレスポンスの適切なキャッシュ
- **SEO対応**: メタタグと構造化データ

## 🔒 セキュリティ

- **APIキー保護**: 環境変数での安全な管理
- **入力検証**: サーバーサイドでのデータ検証
- **XSS対策**: Reactの自動エスケープ機能
- **CSRF対策**: Next.jsの組み込みセキュリティ機能

## 🤝 コントリビューション

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

## 📄 ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## 🆘 サポート

問題や質問がある場合は、GitHubのIssuesでお知らせください。

---

**Smokers Oasis Map** - 喫煙者のためのオアシスを地図上で 🚬🗺️
