// More Rewards' web half. Runs in Root.html, the document every game page iframe sits in. When the end-of-day
// rewards page (RogueSelect.html) shows more than the game's three cards, it lays them out in even rows of at most
// five, makes the cards smaller once there is more than one row, and lets the cards scroll when they still do not fit,
// so no card is ever cut off. With three cards or fewer the page is left exactly as the game draws it. The plugin
// collects notes for its log through window.__slmorerewards.take().
(function () {
  if (window.__slmorerewards) return;

  var GAME_CARDS = 3;
  var MOST_PER_ROW = 5;
  var CHECK_MS = 250;
  var STYLE_ID = 'slmr-style';
  var MANY = 'slmr-many';
  var COMPACT = 'slmr-compact';
  var COLUMNS = '--slmr-columns';
  // The game's page is a flex column (.panel > .content) with the card grid between the title and the footer. Many
  // cards: the grid takes only the height left over and scrolls inside it, and its columns come from --slmr-columns.
  // A scrolling box clips what spills over its edges, so the padding holds what the game's cards draw outside
  // themselves: the hover lift and glow (6px up, 32px glow, 34px on an exclusive card), the buy flash (scaled to 1.04
  // with a 24px glow) and the bought stamp (10px past the right edge). Compact: smaller cards, so two rows fit a 1080p
  // screen, and at five across (about 240px a card) long names, costs and button labels wrap inside the card.
  var CSS = [
    'html.slmr-many .content{min-height:0}',
    'html.slmr-many .card-grid{flex:0 1 auto;min-height:0;overflow-x:hidden;overflow-y:auto;' +
      'grid-template-columns:repeat(var(--slmr-columns),minmax(0,320px));justify-content:center;gap:16px;padding:42px 36px 36px}',
    'html.slmr-many .card-grid::-webkit-scrollbar{width:8px}',
    'html.slmr-many .card-grid::-webkit-scrollbar-track{background:rgba(0,0,0,.25);border-radius:4px}',
    'html.slmr-many .card-grid::-webkit-scrollbar-thumb{background:rgba(255,200,80,.35);border-radius:4px}',
    'html.slmr-compact .card-slot{display:flex}',
    'html.slmr-compact .card{flex:1;min-height:0;padding:16px 16px 16px}',
    'html.slmr-compact .icon-frame{width:64px;height:64px;margin-bottom:10px}',
    'html.slmr-compact .card-name{font-size:16px;letter-spacing:0;overflow-wrap:anywhere;margin-bottom:8px;padding-bottom:8px}',
    'html.slmr-compact .card-desc{font-size:13px;line-height:1.5;overflow-wrap:anywhere;margin-bottom:10px}',
    'html.slmr-compact .card-cost{overflow-wrap:anywhere;margin-bottom:8px}',
    'html.slmr-compact .card-btn{font-size:14px;letter-spacing:2px;white-space:normal;overflow-wrap:anywhere;padding:9px 6px 10px}'
  ].join('\n');

  var notes = [];
  var shownShape = '';
  var hasReportedAccess = false;
  var lastFailure = '';

  // Rows of at most five, as even as they can be: 6 cards are 3 + 3, 7 are 4 + 3, 10 are 5 + 5, 11 are 4 + 4 + 3.
  function layout(cardCount) {
    if (cardCount <= GAME_CARDS) return null;

    var rows = Math.ceil(cardCount / MOST_PER_ROW);

    return { rows: rows, columns: Math.ceil(cardCount / rows) };
  }

  function arrange(doc) {
    var root = doc.documentElement;
    var grid = doc.querySelector('.card-grid');
    var cardCount = grid ? grid.querySelectorAll('.card-slot').length : 0;
    var shape = layout(cardCount);

    if (!shape) {
      root.classList.remove(MANY);
      root.classList.remove(COMPACT);
      shownShape = '';

      return;
    }

    addStyle(doc);
    root.classList.add(MANY);

    if (shape.rows > 1) root.classList.add(COMPACT);
    else root.classList.remove(COMPACT);

    root.style.setProperty(COLUMNS, String(shape.columns));
    noteShape(cardCount, shape);
  }

  function noteShape(cardCount, shape) {
    var key = cardCount + ':' + shape.columns;

    if (key === shownShape) return;

    shownShape = key;
    notes.push('showing ' + cardCount + ' rewards in ' + shape.rows + (shape.rows === 1 ? ' row' : ' rows') + ' of up to ' + shape.columns);
  }

  // The page may reload and lose the style, so it is looked for on every check; it is never added twice.
  function addStyle(doc) {
    if (doc.getElementById(STYLE_ID)) return;

    var style = doc.createElement('style');

    style.id = STYLE_ID;
    style.textContent = CSS;
    doc.head.appendChild(style);
  }

  // The rewards page is a same-origin iframe, so its document can be reached from here; polled because the game
  // creates and drops that iframe whenever it likes.
  function check() {
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      if (!/RogueSelect\.html/.test(frames[i].src || '')) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc || !doc.documentElement) {
        if (!hasReportedAccess) notes.push('cannot reach the rewards page document (root ' + location.protocol + ', iframe ' + frames[i].src + ')');

        hasReportedAccess = true;

        continue;
      }

      arrange(doc);
    }
  }

  function take() {
    var text = notes.join('\n');

    notes = [];

    return text || 'ok';
  }

  setInterval(function () {
    try {
      check();
    } catch (e) {
      var failure = 'check failed: ' + e;

      if (failure !== lastFailure) notes.push(failure);

      lastFailure = failure;
    }
  }, CHECK_MS);

  window.__slmorerewards = { take: take, check: check, layout: layout };
})();
