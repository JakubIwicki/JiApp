import { useState, useCallback, useRef, useEffect } from 'react';
import * as reportService from '../services/reportService';
import type { RevenueReport, ClientReportItem } from '../types/api';

interface UseReportsResult {
  revenueReports: RevenueReport[];
  clientReports: ClientReportItem[];
  isLoading: boolean;
  error: string | null;
  fetchRevenueReport: (
    boardId: number,
    from: string,
    to: string,
    groupBy: string,
  ) => Promise<void>;
  fetchClientReport: (boardId: number, sortBy: string) => Promise<void>;
}

const useReports = (): UseReportsResult => {
  const [revenueReports, setRevenueReports] = useState<RevenueReport[]>([]);
  const [clientReports, setClientReports] = useState<ClientReportItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = abortRef.current;
    return () => {
      controller?.abort();
    };
  }, [abortRef]);

  const fetchRevenueReport = useCallback(
    async (boardId: number, from: string, to: string, groupBy: string) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);
      setError(null);

      try {
        const data = await reportService.getRevenueReport(boardId, from, to, groupBy);
        setRevenueReports(data);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Failed to load revenue report');
        setRevenueReports([]);
      } finally {
        setIsLoading(false);
      }
    },
    [],
  );

  const fetchClientReport = useCallback(
    async (boardId: number, sortBy: string) => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      setIsLoading(true);
      setError(null);

      try {
        const data = await reportService.getClientReport(boardId, sortBy);
        setClientReports(data);
      } catch (err) {
        if (err instanceof Error && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Failed to load client report');
        setClientReports([]);
      } finally {
        setIsLoading(false);
      }
    },
    [],
  );

  return {
    revenueReports,
    clientReports,
    isLoading,
    error,
    fetchRevenueReport,
    fetchClientReport,
  };
};

export default useReports;
