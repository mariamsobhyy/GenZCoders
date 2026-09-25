// ----------------------------------------------------------------------

/**
 * Returns true only for absolute http(s) URLs or server-relative paths
 * (a single leading "/", e.g. "/uploads/file.pdf").
 * Rejects javascript:, data:, protocol-relative ("//host") and anything unparsable.
 */
export function isSafeUrl(url: unknown): boolean {
  if (typeof url !== 'string') return false;

  const value = url.trim();
  if (!value) return false;

  if (value.startsWith('/')) {
    // "//host" and "/\host" are treated by browsers as protocol-relative
    return !value.startsWith('//') && !value.startsWith('/\\');
  }

  try {
    const parsed = new URL(value);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}

/**
 * Opens the URL only when it passes `isSafeUrl`. Returns whether it was opened.
 */
export function openSafeUrl(url: unknown, target = '_blank'): boolean {
  if (!isSafeUrl(url)) {
    console.warn('Blocked opening unsafe URL:', url);
    return false;
  }

  window.open(String(url).trim(), target, 'noopener,noreferrer');
  return true;
}
