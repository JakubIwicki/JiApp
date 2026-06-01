import { use } from 'react';
import { BoardContext } from '../../../context/BoardContext';

export const useBoard = () => {
  const context = use(BoardContext);
  if (!context) throw new Error('useBoard must be used within a BoardProvider');
  return context;
};
