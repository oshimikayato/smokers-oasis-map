import { NextResponse } from "next/server";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

// GET: /api/feedback?spotId=1
export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const spotId = searchParams.get("spotId");
  if (!spotId) return NextResponse.json([], { status: 400 });
  const feedbacks = await prisma.feedback.findMany({
    where: { spotId: Number(spotId) },
    orderBy: { createdAt: "desc" },
  });
  return NextResponse.json(feedbacks);
}

// POST: /api/feedback
export async function POST(req: Request) {
  const data = await req.json();
  if (!data.spotId) return NextResponse.json({ error: "spotId required" }, { status: 400 });
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
}
