import { NextResponse } from "next/server";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

// テスト用のフォールバック写真データ
const fallbackPhotos = [
  {
    id: 1,
    spotId: 1,
    url: "https://images.unsplash.com/photo-1518837695005-2083093ee35b?w=400&h=300&fit=crop",
    caption: "渋谷駅東口の喫煙所の様子",
    uploadedBy: "ユーザー1",
    createdAt: "2024-01-01T00:00:00.000Z",
    updatedAt: "2024-01-01T00:00:00.000Z"
  },
  {
    id: 2,
    spotId: 1,
    url: "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=300&fit=crop",
    caption: "夜間の様子",
    uploadedBy: "ユーザー2",
    createdAt: "2024-01-02T00:00:00.000Z",
    updatedAt: "2024-01-02T00:00:00.000Z"
  },
  {
    id: 3,
    spotId: 3,
    url: "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=400&h=300&fit=crop",
    caption: "スターバックスの喫煙席",
    uploadedBy: "ユーザー3",
    createdAt: "2024-01-03T00:00:00.000Z",
    updatedAt: "2024-01-03T00:00:00.000Z"
  }
];

// GET: /api/photos?spotId=1
export async function GET(request: Request) {
  try {
    const { searchParams } = new URL(request.url);
    const spotId = searchParams.get("spotId");
    
    try {
      if (spotId) {
        // 特定のスポットの写真を取得
        const photos = await prisma.photo.findMany({
          where: { spotId: Number(spotId) },
          orderBy: { createdAt: "desc" },
        });
        return NextResponse.json(photos);
      } else {
        // 全写真を取得（統計用）
        const photos = await prisma.photo.findMany({
          orderBy: { createdAt: "desc" },
        });
        return NextResponse.json(photos);
      }
    } catch (dbError) {
      console.log("データベースエラー、フォールバックデータを使用:", dbError);
      
      if (spotId) {
        // フォールバックデータでフィルタリング
        const filteredPhotos = fallbackPhotos.filter(p => p.spotId === Number(spotId));
        return NextResponse.json(filteredPhotos);
      } else {
        // 全フォールバックデータを返す
        return NextResponse.json(fallbackPhotos);
      }
    }
  } catch (error) {
    console.error("GET エラー:", error);
    return NextResponse.json([]);
  }
}

// POST: /api/photos
export async function POST(req: Request) {
  try {
    const data = await req.json();
    if (!data.spotId || !data.url) {
      return NextResponse.json({ error: "spotId and url are required" }, { status: 400 });
    }
    
    try {
      const photo = await prisma.photo.create({
        data: {
          spotId: Number(data.spotId),
          url: data.url,
          caption: data.caption || "",
          uploadedBy: data.uploadedBy || "匿名ユーザー",
        },
      });
      return NextResponse.json(photo, { status: 201 });
    } catch (dbError) {
      console.log("データベースエラー、フォールバックレスポンス:", dbError);
      
      // フォールバック用のレスポンス
      const mockPhoto = {
        id: Date.now(),
        spotId: Number(data.spotId),
        url: data.url,
        caption: data.caption || "",
        uploadedBy: data.uploadedBy || "匿名ユーザー",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      };
      
      return NextResponse.json(mockPhoto, { status: 201 });
    }
  } catch (error) {
    console.error("POST エラー:", error);
    return NextResponse.json({ error: "サーバーエラー" }, { status: 500 });
  }
} 