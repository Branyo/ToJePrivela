const CARD_WIDTH = 84;
const CARD_HEIGHT = 116;

/**
 * Throws a bad card from one element to another along a spinning arc. Resolves when the card lands,
 * or right away when the user prefers reduced motion.
 */
export function flyCard(from: Element, to: Element, badPoints: number, durationMs = 950): Promise<void> {
  const reduceMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

  if (reduceMotion || typeof document.body.animate !== 'function') {
    return Promise.resolve();
  }

  const start = from.getBoundingClientRect();
  const end = to.getBoundingClientRect();
  const startX = start.left + start.width / 2 - CARD_WIDTH / 2;
  const startY = start.top + start.height / 2 - CARD_HEIGHT / 2;
  const dx = end.left + end.width / 2 - CARD_WIDTH / 2 - startX;
  const dy = end.top + end.height / 2 - CARD_HEIGHT / 2 - startY;
  const arc = Math.min(-120, -Math.abs(dx) * 0.35);

  const card = document.createElement('div');
  card.className = 'flying-card';
  card.setAttribute('aria-hidden', 'true');
  card.innerHTML = `<span>🫏</span><small>×${badPoints}</small>`;
  card.style.left = `${startX}px`;
  card.style.top = `${startY}px`;
  document.body.appendChild(card);

  const animation = card.animate(
    [
      { transform: 'translate(0, 0) rotate(0deg) scale(1)', offset: 0 },
      { transform: 'translate(0, -30px) rotate(-12deg) scale(1.25)', offset: 0.15 },
      { transform: `translate(${dx * 0.5}px, ${dy * 0.5 + arc}px) rotate(200deg) scale(1.1)`, offset: 0.55 },
      { transform: `translate(${dx}px, ${dy}px) rotate(720deg) scale(0.35)`, offset: 1 },
    ],
    { duration: durationMs, easing: 'cubic-bezier(0.45, 0, 0.3, 1)', fill: 'forwards' },
  );

  const land = () => card.remove();
  return animation.finished.then(land, land);
}
