-- CreateTable
CREATE TABLE "Feedback" (
    "id" SERIAL NOT NULL,
    "spotId" INTEGER NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,
    "found" BOOLEAN,
    "rating" INTEGER,
    "comment" TEXT,
    "reportType" TEXT,

    CONSTRAINT "Feedback_pkey" PRIMARY KEY ("id")
);

-- AddForeignKey
ALTER TABLE "Feedback" ADD CONSTRAINT "Feedback_spotId_fkey" FOREIGN KEY ("spotId") REFERENCES "SmokingSpot"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
