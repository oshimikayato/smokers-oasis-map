import { NextResponse } from "next/server";
import { PrismaClient } from "@prisma/client";

const prisma = new PrismaClient();

export async function GET(request: Request) {
  // クエリパラメータでカテゴリ・タグ絞り込み
  const { searchParams } = new URL(request.url);
  const category = searchParams.get("category");
  const tag = searchParams.get("tag");
  const keyword = searchParams.get("q");

  const where: any = {};
  if (category) where.category = category;
  if (tag) where.tags = { has: tag };
  if (keyword) {
    where.OR = [
      { name: { contains: keyword } },
      { address: { contains: keyword } },
      { description: { contains: keyword } },
    ];
  }

  const spots = await prisma.smokingSpot.findMany({ where });
  return NextResponse.json(spots);
}

export async function POST(req: Request) {
  const data = await req.json();
  const newSpot = await prisma.smokingSpot.create({
    data: {
      name: data.name,
      lat: parseFloat(data.lat),
      lng: parseFloat(data.lng),
      address: data.address,
      description: data.description,
      category: data.category || "喫煙所",
      tags: data.tags || [],
    },
  });
  return NextResponse.json(newSpot, { status: 201 });
}
