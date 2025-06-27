"use client";
import React, { createContext, useContext, useEffect, useState } from 'react';
import { SmokingSpotWithDistance } from '@/types';

interface FavoritesContextType {
  favorites: SmokingSpotWithDistance[];
  addFavorite: (spot: SmokingSpotWithDistance) => void;
  removeFavorite: (spotId: number) => void;
  isFavorite: (spotId: number) => boolean;
  toggleFavorite: (spot: SmokingSpotWithDistance) => void;
  clearFavorites: () => void;
}

const FavoritesContext = createContext<FavoritesContextType | undefined>(undefined);

export function FavoritesProvider({ children }: { children: React.ReactNode }) {
  const [favorites, setFavorites] = useState<SmokingSpotWithDistance[]>([]);

  useEffect(() => {
    // ローカルストレージからお気に入りを読み込み
    const savedFavorites = localStorage.getItem('favorites');
    if (savedFavorites) {
      try {
        setFavorites(JSON.parse(savedFavorites));
      } catch (error) {
        console.error('お気に入りの読み込みに失敗しました:', error);
      }
    }
  }, []);

  useEffect(() => {
    // お気に入りが変更されたらローカルストレージに保存
    localStorage.setItem('favorites', JSON.stringify(favorites));
  }, [favorites]);

  const addFavorite = (spot: SmokingSpotWithDistance) => {
    setFavorites(prev => {
      if (!prev.find(fav => fav.id === spot.id)) {
        return [...prev, spot];
      }
      return prev;
    });
  };

  const removeFavorite = (spotId: number) => {
    setFavorites(prev => prev.filter(fav => fav.id !== spotId));
  };

  const isFavorite = (spotId: number) => {
    return favorites.some(fav => fav.id === spotId);
  };

  const toggleFavorite = (spot: SmokingSpotWithDistance) => {
    if (isFavorite(spot.id)) {
      removeFavorite(spot.id);
    } else {
      addFavorite(spot);
    }
  };

  const clearFavorites = () => {
    setFavorites([]);
  };

  return (
    <FavoritesContext.Provider value={{
      favorites,
      addFavorite,
      removeFavorite,
      isFavorite,
      toggleFavorite,
      clearFavorites
    }}>
      {children}
    </FavoritesContext.Provider>
  );
}

export function useFavorites() {
  const context = useContext(FavoritesContext);
  if (context === undefined) {
    throw new Error('useFavorites must be used within a FavoritesProvider');
  }
  return context;
} 