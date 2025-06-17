-- CreateTable
CREATE TABLE "SmokingSpot" (
    "id" SERIAL NOT NULL,
    "name" TEXT NOT NULL,
    "lat" DOUBLE PRECISION NOT NULL,
    "lng" DOUBLE PRECISION NOT NULL,
    "address" TEXT,
    "description" TEXT,
    "category" TEXT NOT NULL,
    "tags" TEXT[],

    CONSTRAINT "SmokingSpot_pkey" PRIMARY KEY ("id")
);
