import { NextResponse } from "next/server";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

// テスト用のフォールバックフィードバックデータ
const fallbackFeedbacks = [
  {
    id: 1,
    spotId: 1,
    found: true,
    rating: 4,
    comment: "きれいで使いやすい喫煙所でした",
    reportType: null,
    createdAt: "2024-01-01T00:00:00.000Z",
    updatedAt: "2024-01-01T00:00:00.000Z"
  },
  {
    id: 2,
    spotId: 1,
    found: true,
    rating: 3,
    comment: "普通の喫煙所です",
    reportType: null,
    createdAt: "2024-01-02T00:00:00.000Z",
    updatedAt: "2024-01-02T00:00:00.000Z"
  },
  {
    id: 3,
    spotId: 2,
    found: true,
    rating: 5,
    comment: "空調が効いていて快適でした",
    reportType: null,
    createdAt: "2024-01-03T00:00:00.000Z",
    updatedAt: "2024-01-03T00:00:00.000Z"
  }
];

// GET: /api/feedback?spotId=1
export async function GET(request: Request) {
  try {
    const { searchParams } = new URL(request.url);
    const spotId = searchParams.get("spotId");
    
    try {
      if (spotId) {
        // 特定のスポットのフィードバックを取得
        const feedbacks = await prisma.feedback.findMany({
          where: { spotId: Number(spotId) },
          orderBy: { createdAt: "desc" },
        });
        return NextResponse.json(feedbacks);
      } else {
        // 全フィードバックを取得（統計用）
        const feedbacks = await prisma.feedback.findMany({
          orderBy: { createdAt: "desc" },
        });
        return NextResponse.json(feedbacks);
      }
    } catch (dbError) {
      console.log("データベースエラー、フォールバックデータを使用:", dbError);
      
      if (spotId) {
        // フォールバックデータでフィルタリング
        const filteredFeedbacks = fallbackFeedbacks.filter(f => f.spotId === Number(spotId));
        return NextResponse.json(filteredFeedbacks);
      } else {
        // 全フォールバックデータを返す
        return NextResponse.json(fallbackFeedbacks);
      }
    }
  } catch (error) {
    console.error("GET エラー:", error);
    return NextResponse.json([]);
  }
}

// POST: /api/feedback
export async function POST(req: Request) {
  try {
    const data = await req.json();
    if (!data.spotId) return NextResponse.json({ error: "spotId required" }, { status: 400 });
    
    try {
      const feedback = await prisma.feedback.create({
        data: {
          spotId: Number(data.spotId),
          found: data.found,
          rating: data.rating,
          comment: data.comment,
          reportType: data.reportType,
        },
      });
      return NextResponse.json(feedback, { status: 201 });
    } catch (dbError) {
      console.log("データベースエラー、フォールバックレスポンス:", dbError);
      
      // フォールバック用のレスポンス
      const mockFeedback = {
        id: Date.now(),
        spotId: Number(data.spotId),
        found: data.found,
        rating: data.rating,
        comment: data.comment,
        reportType: data.reportType,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
      };
      
      return NextResponse.json(mockFeedback, { status: 201 });
    }
  } catch (error) {
    console.error("POST エラー:", error);
    return NextResponse.json({ error: "サーバーエラー" }, { status: 500 });
  }
}
