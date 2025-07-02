import { NextResponse } from "next/server";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

// POST: /api/feedback/like
// body: { feedbackId: number, userId: string }
export async function POST(req: Request) {
  try {
    const { feedbackId, userId } = await req.json();
    if (!feedbackId || !userId) {
      return NextResponse.json({ error: "feedbackIdとuserIdは必須です" }, { status: 400 });
    }
    // すでにいいね済みか確認
    const existing = await prisma.feedbackLike.findFirst({ where: { feedbackId, userId } });
    if (existing) {
      return NextResponse.json({ message: "すでにいいね済みです" }, { status: 200 });
    }
    const like = await prisma.feedbackLike.create({ data: { feedbackId, userId } });
    return NextResponse.json(like, { status: 201 });
  } catch (error) {
    console.error("POST /feedback/like エラー:", error);
    return NextResponse.json({ error: "サーバーエラー" }, { status: 500 });
  }
}

// GET: /api/feedback/like?feedbackId=xxx
export async function GET(request: Request) {
  try {
    const { searchParams } = new URL(request.url);
    const feedbackId = searchParams.get("feedbackId");
    if (!feedbackId) {
      return NextResponse.json({ error: "feedbackIdは必須です" }, { status: 400 });
    }
    const count = await prisma.feedbackLike.count({ where: { feedbackId: Number(feedbackId) } });
    return NextResponse.json({ feedbackId: Number(feedbackId), likeCount: count });
  } catch (error) {
    console.error("GET /feedback/like エラー:", error);
    return NextResponse.json({ error: "サーバーエラー" }, { status: 500 });
  }
} 