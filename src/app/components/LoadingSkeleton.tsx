import React from 'react';

interface LoadingSkeletonProps {
  type?: 'spot' | 'map' | 'list';
  count?: number;
}

const LoadingSkeleton: React.FC<LoadingSkeletonProps> = ({ type = 'spot', count = 3 }) => {
  if (type === 'map') {
    return (
      <div className="animate-pulse">
        <div className="bg-gray-200 rounded-lg h-96 w-full"></div>
      </div>
    );
  }

  if (type === 'list') {
    return (
      <div className="space-y-4">
        {Array.from({ length: count }).map((_, index) => (
          <div key={index} className="animate-pulse">
            <div className="bg-white rounded-lg p-4 shadow-sm">
              <div className="flex items-start space-x-4">
                <div className="bg-gray-200 rounded-full w-12 h-12"></div>
                <div className="flex-1 space-y-2">
                  <div className="bg-gray-200 h-4 w-3/4 rounded"></div>
                  <div className="bg-gray-200 h-3 w-1/2 rounded"></div>
                  <div className="bg-gray-200 h-3 w-2/3 rounded"></div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  // Default spot skeleton
  return (
    <div className="animate-pulse">
      <div className="bg-white rounded-lg p-4 shadow-sm">
        <div className="flex items-start space-x-4">
          <div className="bg-gray-200 rounded-full w-12 h-12"></div>
          <div className="flex-1 space-y-2">
            <div className="bg-gray-200 h-4 w-3/4 rounded"></div>
            <div className="bg-gray-200 h-3 w-1/2 rounded"></div>
            <div className="bg-gray-200 h-3 w-2/3 rounded"></div>
            <div className="flex space-x-2">
              <div className="bg-gray-200 h-6 w-16 rounded-full"></div>
              <div className="bg-gray-200 h-6 w-20 rounded-full"></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoadingSkeleton; 