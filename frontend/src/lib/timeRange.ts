import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { format } from 'date-fns';

/** Preset keys stored in ?range= (DBA Dash–style relative windows). */
export const TIME_RANGE_PRESETS: { label: string; value: string }[] = [
  { label: 'Last 5 min', value: '5m' },
  { label: 'Last 10 min', value: '10m' },
  { label: 'Last 15 min', value: '15m' },
  { label: 'Last 30 min', value: '30m' },
  { label: 'Last 1h', value: '1h' },
  { label: 'Last 2h', value: '2h' },
  { label: 'Last 4h', value: '4h' },
  { label: 'Last 8h', value: '8h' },
  { label: 'Last 12h', value: '12h' },
  { label: 'Last 24h', value: '24h' },
  { label: 'Last 3d', value: '3d' },
  { label: 'Last 7d', value: '7d' },
  { label: 'Last 14d', value: '14d' },
  { label: 'Last 30d', value: '30d' },
];

const PRESET_MS: Record<string, number> = {
  '5m': 5 * 60 * 1000,
  '10m': 10 * 60 * 1000,
  '15m': 15 * 60 * 1000,
  '30m': 30 * 60 * 1000,
  '1h': 60 * 60 * 1000,
  '2h': 2 * 60 * 60 * 1000,
  '4h': 4 * 60 * 60 * 1000,
  '8h': 8 * 60 * 60 * 1000,
  '12h': 12 * 60 * 60 * 1000,
  '24h': 24 * 60 * 60 * 1000,
  '3d': 3 * 24 * 60 * 60 * 1000,
  '7d': 7 * 24 * 60 * 60 * 1000,
  '14d': 14 * 24 * 60 * 60 * 1000,
  '30d': 30 * 24 * 60 * 60 * 1000,
};

const DEFAULT_RANGE = '24h';
const MAX_SPAN_MS = 30 * 24 * 60 * 60 * 1000; // align with longest preset

export type ParsedTimeRange = {
  fromUtc: Date;
  toUtc: Date;
  /** `custom` or preset key e.g. `5m`, `24h` */
  mode: string;
  /** Short label for UI */
  label: string;
};

function clampWindow(from: Date, to: Date): { fromUtc: Date; toUtc: Date } {
  let t = to.getTime() > Date.now() ? new Date() : to;
  let f = from;
  if (f >= t) f = new Date(t.getTime() - 60_000);
  if (t.getTime() - f.getTime() > MAX_SPAN_MS) f = new Date(t.getTime() - MAX_SPAN_MS);
  if (t.getTime() - f.getTime() < 60_000) f = new Date(t.getTime() - 60_000);
  return { fromUtc: f, toUtc: t };
}

function safeParseIso(s: string | null): Date | null {
  if (!s?.trim()) return null;
  const d = new Date(s);
  return Number.isNaN(d.getTime()) ? null : d;
}

/**
 * Parse global time range from URL (?from= & ?to= ISO UTC, or ?range=preset).
 */
export function parseTimeRangeFromSearchParams(sp: URLSearchParams): ParsedTimeRange {
  const fromQ = safeParseIso(sp.get('from'));
  const toQ = safeParseIso(sp.get('to'));
  if (fromQ && toQ) {
    const { fromUtc, toUtc } = clampWindow(fromQ, toQ);
    return {
      fromUtc,
      toUtc,
      mode: 'custom',
      label: `${format(fromUtc, 'MMM d HH:mm')} – ${format(toUtc, 'MMM d HH:mm')}`,
    };
  }

  const range = sp.get('range') || DEFAULT_RANGE;
  const ms = PRESET_MS[range];
  const toUtc = new Date();
  if (!ms) {
    const fallbackMs = PRESET_MS[DEFAULT_RANGE]!;
    const fromUtc = new Date(toUtc.getTime() - fallbackMs);
    return { fromUtc, toUtc, mode: DEFAULT_RANGE, label: TIME_RANGE_PRESETS.find(p => p.value === DEFAULT_RANGE)!.label };
  }
  const fromUtc = new Date(toUtc.getTime() - ms);
  const label = TIME_RANGE_PRESETS.find(p => p.value === range)?.label || range;
  return { fromUtc, toUtc, mode: range, label };
}

export function useParsedTimeRange(): ParsedTimeRange {
  const [sp] = useSearchParams();
  const qs = sp.toString();
  return useMemo(() => parseTimeRangeFromSearchParams(new URLSearchParams(qs)), [qs]);
}

/** Query params for APIs that support fromUtc / toUtc. */
export function useApiTimeWindow(): { fromUtc: string; toUtc: string } {
  const r = useParsedTimeRange();
  return useMemo(() => ({ fromUtc: r.fromUtc.toISOString(), toUtc: r.toUtc.toISOString() }), [r.fromUtc, r.toUtc]);
}

/** Axis / subtitle label: "5m" … "14d" or "36h" for custom spans. */
export function rangeSpanLabel(from: Date, to: Date): string {
  const h = (to.getTime() - from.getTime()) / 3600000;
  if (h < 1) return `${Math.round(h * 60)}m`;
  if (h < 48) return `${Math.round(h * 10) / 10}h`;
  return `${Math.round(h / 24)}d`;
}
