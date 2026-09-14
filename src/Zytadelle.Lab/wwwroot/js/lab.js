/*
 * Clipboard, with a fallback. A tuning set is the one thing that has to leave this window, and the
 * web view does not always grant the async clipboard - so both calls fall back to the old
 * execCommand path and report whether anything actually happened.
 */
window.labClipboard = {
  async write(text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      const area = document.createElement('textarea');
      area.value = text;
      area.style.position = 'fixed';
      area.style.opacity = '0';
      document.body.append(area);
      area.select();
      let ok = false;
      try {
        ok = document.execCommand('copy');
      } catch {
        ok = false;
      }
      area.remove();
      return ok;
    }
  },

  async read() {
    try {
      return await navigator.clipboard.readText();
    } catch {
      return window.prompt('Paste a tuning set:') ?? '';
    }
  },
};
