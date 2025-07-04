import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { ThemeProvider } from "@/contexts/ThemeContext";
import { FavoritesProvider } from "@/contexts/FavoritesContext";
import { HistoryProvider } from "@/contexts/HistoryContext";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Smokers Oasis - 喫煙所・喫煙可能店舗マップ",
  description: "近くの喫煙所や喫煙可能な飲食店を簡単に見つけられるマップアプリ",
  keywords: "喫煙所, 喫煙, マップ, 飲食店, 分煙",
  authors: [{ name: "Smokers Oasis Team" }],
  viewport: "width=device-width, initial-scale=1",
  robots: "index, follow",
  openGraph: {
    title: "Smokers Oasis - 喫煙所・喫煙可能店舗マップ",
    description: "近くの喫煙所や喫煙可能な飲食店を簡単に見つけられるマップアプリ",
    type: "website",
    locale: "ja_JP",
  },
  twitter: {
    card: "summary_large_image",
    title: "Smokers Oasis - 喫煙所・喫煙可能店舗マップ",
    description: "近くの喫煙所や喫煙可能な飲食店を簡単に見つけられるマップアプリ",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ja" suppressHydrationWarning>
      <head>
        <link rel="icon" type="image/png" href="/favicon.png" />
      </head>
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <ThemeProvider>
          <FavoritesProvider>
            <HistoryProvider>
              {/* スキップリンク - アクセシビリティ向上 */}
              <a href="#main-content" className="skip-link">
                メインコンテンツにスキップ
              </a>
              
              <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-100 dark:from-slate-900 dark:via-slate-800 dark:to-slate-900">
                {children}
              </div>
            </HistoryProvider>
          </FavoritesProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
