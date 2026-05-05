import { useState, useRef, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Clock, ChevronDown } from 'lucide-react';
import { clsx } from 'clsx';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import { usePresentationOptional } from '../context/PresentationContext';
import { parseTimeRangeFromSearchParams, TIME_RANGE_PRESETS } from '../lib/timeRange';

function utcIsoToLocalDatetimeValue(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export default function TimeRangePicker() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [open, setOpen] = useState(false);
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');
  const ref = useRef<HTMLDivElement>(null);
  const parsed = parseTimeRangeFromSearchParams(searchParams);
  const { isDesktopData } = usePresentationOptional();
  const reduceMotion = useReducedMotion();

  useEffect(() => {
    if (!open) return;
    const p = parseTimeRangeFromSearchParams(searchParams);
    setCustomFrom(utcIsoToLocalDatetimeValue(p.fromUtc.toISOString()));
    setCustomTo(utcIsoToLocalDatetimeValue(p.toUtc.toISOString()));
  }, [open, searchParams]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const selectPreset = (value: string) => {
    const params = new URLSearchParams(searchParams);
    params.set('range', value);
    params.delete('from');
    params.delete('to');
    setSearchParams(params);
    setOpen(false);
  };

  const applyCustom = () => {
    const fromD = new Date(customFrom);
    const toD = new Date(customTo);
    if (Number.isNaN(fromD.getTime()) || Number.isNaN(toD.getTime()) || fromD >= toD) return;
    const params = new URLSearchParams(searchParams);
    params.delete('range');
    params.set('from', fromD.toISOString());
    params.set('to', toD.toISOString());
    setSearchParams(params);
    setOpen(false);
  };

  const currentRange = searchParams.get('range');
  const hasCustom = searchParams.has('from') && searchParams.has('to');
  const currentPreset = hasCustom ? null : (currentRange || '24h');
  const currentLabel = parsed.label;

  const presetButtons = (motionItems: boolean) =>
    TIME_RANGE_PRESETS.map((p, i) => {
      const active = !hasCustom && currentPreset === p.value;
      const cls = clsx(
        'w-full px-3 py-1.5 text-left text-xs transition-colors duration-200',
        active ? 'bg-blue-500/15 text-blue-400' : 'text-gray-400 hover:text-white hover:bg-slate-800/50',
      );
      if (motionItems) {
        return (
          <motion.button
            key={p.value}
            type="button"
            initial={{ opacity: 0, x: -6 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: i * 0.02, type: 'spring', stiffness: 400, damping: 30 }}
            onClick={() => selectPreset(p.value)}
            className={cls}
          >
            {p.label}
          </motion.button>
        );
      }
      return (
        <button key={p.value} type="button" onClick={() => selectPreset(p.value)} className={cls}>
          {p.label}
        </button>
      );
    });

  const menuClass =
    'absolute right-0 top-full mt-1 glass-strong rounded-lg shadow-xl py-2 min-w-[220px] max-h-[min(70vh,520px)] overflow-y-auto z-50';

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs text-gray-400 hover:text-white hover:bg-slate-800/50 transition-all duration-200 active:scale-[0.98] max-w-[200px]"
        title={parsed.mode === 'custom' ? `${parsed.fromUtc.toISOString()} – ${parsed.toUtc.toISOString()}` : undefined}
      >
        <Clock className="w-3.5 h-3.5 shrink-0" />
        <span className="truncate">{currentLabel}</span>
        {!reduceMotion && !isDesktopData ? (
          <motion.span
            animate={{ rotate: open ? 180 : 0 }}
            transition={{ type: 'spring', stiffness: 400, damping: 26 }}
            className="inline-flex shrink-0"
          >
            <ChevronDown className="w-3 h-3" />
          </motion.span>
        ) : (
          <ChevronDown
            className={clsx('w-3 h-3 shrink-0 transition-transform duration-200', open && 'rotate-180')}
          />
        )}
      </button>
      <AnimatePresence>
        {open && !isDesktopData && !reduceMotion && (
          <motion.div
            key="time-range-menu"
            className={clsx(menuClass, 'origin-top-right overflow-hidden gpu-promote-layer')}
            initial={{ opacity: 0, y: -6, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -4, scale: 0.98 }}
            transition={{ type: 'spring', stiffness: 480, damping: 34 }}
          >
            <div className="px-2 pb-1 text-[10px] uppercase tracking-wider text-gray-500">Presets</div>
            {presetButtons(true)}
            <div className="my-2 mx-2 border-t border-white/10" />
            <div className="px-3 pb-1 text-[10px] uppercase tracking-wider text-gray-500">Custom range</div>
            <div className="px-3 space-y-2 pb-2">
              <label className="block text-[10px] text-gray-500">From (local)</label>
              <input
                type="datetime-local"
                value={customFrom}
                onChange={e => setCustomFrom(e.target.value)}
                className="w-full rounded-md bg-slate-900/80 border border-white/10 px-2 py-1 text-[11px] text-gray-200"
              />
              <label className="block text-[10px] text-gray-500">To (local)</label>
              <input
                type="datetime-local"
                value={customTo}
                onChange={e => setCustomTo(e.target.value)}
                className="w-full rounded-md bg-slate-900/80 border border-white/10 px-2 py-1 text-[11px] text-gray-200"
              />
              <button
                type="button"
                onClick={applyCustom}
                className="w-full mt-1 py-1.5 rounded-md bg-blue-600/80 hover:bg-blue-600 text-xs text-white font-medium"
              >
                Apply range
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
      {open && (isDesktopData || reduceMotion) && (
        <div className={menuClass}>
          <div className="px-2 pb-1 text-[10px] uppercase tracking-wider text-gray-500">Presets</div>
          {presetButtons(false)}
          <div className="my-2 mx-2 border-t border-white/10" />
          <div className="px-3 pb-1 text-[10px] uppercase tracking-wider text-gray-500">Custom range</div>
          <div className="px-3 space-y-2 pb-2">
            <label className="block text-[10px] text-gray-500">From (local)</label>
            <input
              type="datetime-local"
              value={customFrom}
              onChange={e => setCustomFrom(e.target.value)}
              className="w-full rounded-md bg-slate-900/80 border border-white/10 px-2 py-1 text-[11px] text-gray-200"
            />
            <label className="block text-[10px] text-gray-500">To (local)</label>
            <input
              type="datetime-local"
              value={customTo}
              onChange={e => setCustomTo(e.target.value)}
              className="w-full rounded-md bg-slate-900/80 border border-white/10 px-2 py-1 text-[11px] text-gray-200"
            />
            <button
              type="button"
              onClick={applyCustom}
              className="w-full mt-1 py-1.5 rounded-md bg-blue-600/80 hover:bg-blue-600 text-xs text-white font-medium"
            >
              Apply range
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
