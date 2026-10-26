// More Rewards' web half. Runs in Root.html, the document every game page iframe sits in. On the end-of-day rewards
// page (RogueSelect.html) it:
// - lays out more than the game's three cards in even rows of at most five, smaller once there is more than one row,
//   and lets the cards scroll when they still do not fit, so no card is ever cut off;
// - adds the mod's buttons to the cards (ban, and a permanent, one-time or how-long label) and to the footer (the
//   lock count, tomorrow's interest, the cash-out note, the shop, the ban list and the settings), and draws the shop,
//   the ban list, the settings and short notices;
// - takes over the lock button when the player may lock more than one card.
// The plugin tells the page what to draw through window.__slmorerewards.setup/shop/bans/settings/toast, collects notes for its log
// through take(), and hears the buttons through vuplex.postMessage("slmod|morerewards|<verb>|<args>"). The page draws
// only what the plugin said; every click is checked again by the plugin, which owns the points and the cards.
(function () {
  if (window.__slmorerewards) return;

  var GAME_CARDS = 3;
  var MOST_PER_ROW = 5;
  var CHECK_MS = 250;
  var TOAST_MS = 2200;
  var PREFIX = 'slmod|morerewards|';
  var REWARDS_PAGE = 'RogueSelect.html';
  var STYLE_ID = 'slmr-style';
  var MANY = 'slmr-many';
  var COMPACT = 'slmr-compact';
  var LOCKS_LEFT = 'slmr-locks';
  var LOCKS_FULL = 'slmr-full';
  var COLUMNS = '--slmr-columns';
  var LOCK = '\uD83D\uDD12';
  var DOT = ' \u00B7 ';
  // The game's page is a flex column (.panel > .content) with the card grid between the title and the footer. Many
  // cards: the grid takes only the height left over and scrolls inside it, and its columns come from --slmr-columns.
  // A scrolling box clips what spills over its edges, so the padding holds what the game's cards draw outside
  // themselves: the hover lift and glow (6px up, 32px glow, 34px on an exclusive card), the buy flash (scaled to 1.04
  // with a 24px glow) and the bought stamp (10px past the right edge). Compact: smaller cards, so two rows fit a 1080p
  // screen, and at five across (about 240px a card) long names, costs and button labels wrap inside the card.
  var LAYOUT_CSS = [
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
  ];
  // The game greys every lock button but the locked one once a card is locked; with more locks allowed, the mod lifts
  // that while locks are left and greys the rest itself when they run out. The shop and the notices are fixed to the
  // window, so they sit above the cards however the page scrolls.
  var MOD_CSS = [
    'html.slmr-locks .pin-btn.blocked{cursor:pointer}',
    'html.slmr-locks .pin-btn.blocked svg{opacity:1}',
    'html.slmr-locks .pin-btn.blocked:hover{color:var(--text-main);border-color:var(--border-highlight)}',
    'html.slmr-full .pin-btn{cursor:default}',
    'html.slmr-full .pin-btn svg{opacity:.4}',
    'html.slmr-full .pin-btn.on{cursor:pointer}',
    'html.slmr-full .pin-btn.on svg{opacity:1}',
    '.card .mr-life{position:absolute;top:14px;left:14px;z-index:3;font:11px var(--font-num);letter-spacing:2px;padding:2px 7px;border:1px solid;background:rgba(0,0,0,0.35)}',
    '.card .mr-life.perm{color:#9fd38a;border-color:rgba(140,210,120,0.45)}',
    '.card .mr-life.once{color:#e0b070;border-color:rgba(224,176,112,0.5)}',
    '.card .mr-life.timed{color:#8ec5e8;border-color:rgba(142,197,232,0.5)}',
    '.mr-acts{display:flex;justify-content:space-between;margin-top:10px;gap:8px}',
    '.mr-acts button{flex:1;background:transparent;border:1px solid var(--border-light);color:var(--text-meta);font:12px var(--font-main);letter-spacing:1px;padding:5px 4px;cursor:pointer}',
    '.mr-acts button:hover{color:var(--text-main);border-color:var(--border-highlight)}',
    '.mr-acts button.ban:hover{color:var(--danger);border-color:rgba(255,82,82,0.6)}',
    '.mr-acts button b{font-family:var(--font-num);color:var(--highlight);font-weight:normal;margin-left:3px}',
    '.pin-count{font-family:var(--font-num);color:var(--highlight);margin-left:14px;font-size:14px}',
    '.mr-foot-btns .ghost-btn{margin-left:18px;border-color:var(--border-highlight) !important;color:var(--highlight) !important}',
    '.footer-left{min-width:max-content;overflow:visible}',
    '.mr-foot small{display:block;color:var(--text-meta);font-size:13px;margin-top:2px;white-space:nowrap}',
    '.shop{position:fixed;inset:0;z-index:200;background:rgba(8,6,4,0.82);display:flex;align-items:center;justify-content:center}',
    '.shop[hidden]{display:none}',
    '.shop-card{width:min(1500px,92vw);height:min(900px,88vh);display:grid;grid-template-rows:auto auto auto 1fr auto;border:1px solid var(--border-highlight);background:linear-gradient(180deg,rgba(40,34,26,0.98),rgba(22,18,14,0.98));box-shadow:0 30px 90px rgba(0,0,0,0.8)}',
    '.shop-hd{display:flex;align-items:center;gap:24px;padding:20px 28px 12px}',
    '.shop-hd h2{color:#fff;font-size:30px;letter-spacing:10px;font-weight:bold;margin:0}',
    '.shop-hd .rate{color:var(--text-dim);font-size:15px;line-height:1.5}',
    '.shop-hd .rate b{color:var(--highlight);font-family:var(--font-num)}',
    '.shop-hd .sp{flex:1}',
    '.shop-hd .x{font-size:28px;color:var(--text-dim);cursor:pointer}',
    '.shop-hd .x:hover{color:#fff}',
    '.shop-tools{display:flex;gap:14px;align-items:center;padding:0 28px 14px;border-bottom:1px solid var(--border-divider)}',
    '.shop-tools input{flex:1;background:rgba(0,0,0,0.45);border:1px solid var(--border-light);color:#fff;padding:10px 14px;font:17px var(--font-main);outline:none;user-select:text}',
    '.shop-tools input:focus{border-color:var(--highlight)}',
    '.chips{display:flex;flex-wrap:wrap;gap:6px}',
    '.chips span{padding:6px 12px;border:1px solid var(--border-light);color:var(--text-dim);font-size:14px;cursor:pointer}',
    '.chips span.on{background:rgba(255,200,58,0.15);color:var(--highlight);border-color:var(--border-highlight)}',
    '.chips span i{font-style:normal;font-family:var(--font-num);color:var(--text-meta);margin-left:5px}',
    '.shop-cats{padding:10px 28px}',
    '.shop-list{overflow-y:auto;padding:4px 28px 10px;display:grid;grid-template-columns:1fr 1fr;gap:8px 18px;align-content:start;min-height:0}',
    '.shop-list::-webkit-scrollbar{width:8px}',
    '.shop-list::-webkit-scrollbar-thumb{background:rgba(255,200,80,.35)}',
    '.sr{display:grid;grid-template-columns:60px minmax(0,1fr) auto;gap:14px;align-items:center;padding:10px 14px;border:1px solid var(--border-card);background:rgba(40,34,26,0.85)}',
    '.sr img{width:60px;height:60px;object-fit:contain}',
    '.sr .n{color:#fff;font-size:17px;font-weight:bold;display:flex;gap:10px;align-items:center}',
    '.sr .d{color:var(--text-dim);font-size:13px;line-height:1.45;margin-top:3px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}',
    '.sr .why{color:#e0a070;font-size:13px;margin-top:4px}',
    '.sr .early{color:var(--text-meta);font-size:12px;margin-top:4px}',
    '.sr .buy{text-align:right}',
    '.sr .p{font-family:var(--font-num);font-size:22px;color:var(--highlight)}',
    '.sr .p s{color:var(--text-meta);font-size:14px;margin-right:6px}',
    '.sr button{margin-top:6px;background:rgba(255,200,58,0.12);border:1px solid var(--border-highlight);color:var(--highlight);padding:6px 18px;font:bold 14px var(--font-main);letter-spacing:3px;cursor:pointer}',
    '.sr button:hover{background:rgba(255,200,58,0.28);color:#fff}',
    '.sr.grey{opacity:.45}',
    '.sr.grey button{visibility:hidden}',
    '.sr.poor .p{color:var(--danger)}',
    '.sr.poor button{opacity:.4;pointer-events:none}',
    '.sr .tag{font:11px var(--font-num);letter-spacing:2px;padding:1px 6px;border:1px solid;font-weight:normal}',
    '.sr .tag.perm{color:#9fd38a;border-color:rgba(140,210,120,0.45)}',
    '.sr .tag.once{color:#e0b070;border-color:rgba(224,176,112,0.5)}',
    '.sr .tag.timed{color:#8ec5e8;border-color:rgba(142,197,232,0.5)}',
    '.shop-ft{display:flex;align-items:center;gap:20px;padding:14px 28px;border-top:1px solid var(--border-divider);color:var(--text-dim);font-size:15px}',
    '.shop-ft b{color:var(--highlight);font-family:var(--font-num);font-size:20px}',
    '.shop-ft .sp{flex:1}',
    '.shop-none{grid-column:1 / -1;padding:60px;text-align:center;color:var(--text-dim)}',
    '.mr-pane{position:fixed;inset:0;z-index:200;background:rgba(8,6,4,0.82);display:flex;align-items:center;justify-content:center}',
    '.mr-pane[hidden]{display:none}',
    '.mr-pane-card{width:min(1100px,90vw);max-height:88vh;display:flex;flex-direction:column;border:1px solid var(--border-highlight);background:linear-gradient(180deg,rgba(40,34,26,0.98),rgba(22,18,14,0.98));box-shadow:0 30px 90px rgba(0,0,0,0.8)}',
    '.mr-pane-hd{display:flex;align-items:center;padding:20px 28px 8px}',
    '.mr-pane-hd h2{color:#fff;font-size:28px;letter-spacing:8px;font-weight:bold;margin:0}',
    '.mr-pane-hd .sp{flex:1}',
    '.mr-pane-hd .x{font-size:28px;color:var(--text-dim);cursor:pointer;background:none;border:0}',
    '.mr-pane-hd .x:hover{color:#fff}',
    '.mr-pane-note{color:var(--text-dim);font-size:14px;line-height:1.5;padding:0 28px 12px;border-bottom:1px solid var(--border-divider)}',
    '.mr-pane-list{overflow-y:auto;padding:10px 28px 20px;min-height:0}',
    '.mr-pane-list::-webkit-scrollbar{width:8px}',
    '.mr-pane-list::-webkit-scrollbar-thumb{background:rgba(255,200,80,.35)}',
    '.mr-pane-list h3{color:var(--highlight);font-size:15px;letter-spacing:3px;margin:16px 0 6px}',
    '.mr-pane-none{padding:40px;text-align:center;color:var(--text-dim)}',
    '.mr-row{display:flex;align-items:center;gap:16px;padding:10px 14px;border:1px solid var(--border-card);background:rgba(40,34,26,0.85);margin-bottom:6px}',
    '.mr-row .txt{flex:1;min-width:0}',
    '.mr-row .n{color:#fff;font-size:16px}',
    '.mr-row .d{color:var(--text-dim);font-size:13px;line-height:1.45;margin-top:3px}',
    '.mr-row .rs{color:#e0b070;font-size:12px;margin-left:10px}',
    '.mr-row .r{font-family:var(--font-num);color:var(--highlight);font-size:18px}',
    '.mr-row .ctl{display:flex;align-items:center;gap:6px;flex-wrap:wrap;justify-content:flex-end;max-width:45%}',
    '.mr-row button{background:rgba(255,200,58,0.12);border:1px solid var(--border-highlight);color:var(--highlight);padding:6px 14px;font:bold 14px var(--font-main);letter-spacing:2px;cursor:pointer}',
    '.mr-row button:hover{background:rgba(255,200,58,0.28);color:#fff}',
    '.mr-row button.off{background:transparent;color:var(--text-meta);border-color:var(--border-light)}',
    '.mr-row input{width:72px;background:rgba(0,0,0,0.45);border:1px solid var(--border-light);color:#fff;padding:6px 8px;font:16px var(--font-num);text-align:center;outline:none;user-select:text}',
    '.mr-row input:focus{border-color:var(--highlight)}',
    '.toast2{position:fixed;left:50%;top:40px;transform:translateX(-50%);z-index:300;background:rgba(20,16,12,0.95);border:1px solid var(--border-highlight);color:var(--text-main);padding:10px 22px;font-size:16px}'
  ];

  var TEXT = {
    perm: ['PERMANENT', '永久'],
    once: ['ONE-TIME', '一次性'],
    ban: ['Ban', '禁用'],
    lock: ['Lock: keep it through refreshes and into tomorrow', '锁定：刷新和第二天都会保留它'],
    unlock: ['Locked: kept through refreshes and into tomorrow. Click again to unlock.', '已锁定：刷新和第二天都会保留。再点一次解除锁定。'],
    full: ['All locks are in use. Unlock a card first.', '锁定已用完，先解除一张的锁定。'],
    interest: ['Interest tomorrow: +', '明日利息：+'],
    cashOut: ['Leaving cashes out: $', '离开时换成现金：$'],
    shop: ['SHOP', '商 店'],
    pricesNow: ['Prices now: card price ', '当前价格：卡片价格 '],
    eachBuy: ['Each purchase adds ', '每次购买再涨 '],
    day: ['resets every day', '每天重置'],
    week: ['resets every 7 days', '每 7 天重置'],
    never: ['never resets', '不重置'],
    left: ['buys left today: ', '今日还可购买 '],
    search: ['Search rewards by name or what they do', '搜索奖励名称或描述'],
    onlyBuyable: ['Only what I can buy', '只看可购买'],
    own: ['Survival points: ', '拥有生存点：'],
    shopNote: ['Any reward you could be offered can be bought here, even ones no pool would draw today.', '你可能被提供的任何奖励都能在这里购买，即使今天没有被抽到。'],
    buy: ['BUY', '购买'],
    early: ['Bought before its needs are met: double price', '未满足条件提前购买：双倍价格'],
    noBuys: ['No shop buys left today', '今日购买次数已用完'],
    none: ['No reward matches.', '没有匹配的奖励。'],
    bans: ['BANS', '禁用列表'],
    bansNote: ['Removing a ban lets its rewards be offered again and gives back ', '解除禁用后，这些奖励可以再次出现，并退还禁用花费的 '],
    bansNoteEnd: ['% of what it cost.', '%。'],
    noBans: ['Nothing is banned this run.', '本局没有禁用任何奖励。'],
    remove: ['REMOVE', '解除'],
    settings: ['SETTINGS', '设 置'],
    settingsNote: ['More Rewards settings. Changes are saved and apply at once, except the ones marked as needing a restart.', '更多奖励的设置。修改会保存并立即生效，标明需要重启的除外。'],
    restart: ['takes effect after a restart', '重启游戏后生效'],
    on: ['ON', '开'],
    off: ['OFF', '关']
  };

  var SECTIONS = { General: '通用', Rewards: '奖励', Interest: '利息', Locks: '锁定', Cards: '卡片', Shop: '商店' };

  var CATS = [['all', 'All', '全部'], ['body', 'Body', '身体'], ['food', 'Food', '食物'], ['farming', 'Farming', '种植'], ['recipes', 'Recipes', '配方'],
    ['cooking', 'Cooking', '烹饪'], ['traps', 'Traps', '陷阱'], ['trade', 'Trade', '交易'], ['power', 'Power', '电力'], ['airdrops', 'Airdrops', '空投'],
    ['skills', 'Skills', '技能'], ['defense', 'Defense', '防御'], ['storage', 'Storage', '储物'], ['crafting', 'Crafting', '制作'], ['speed', 'Speed', '速度']];

  // The shop's how-long-it-lasts filter, keyed by the kind lifeLabel gives.
  var LIVES = [['all', 'All', '全部'], ['perm', 'Permanent', '永久'], ['once', 'One-time', '一次性'], ['timed', 'Temporary', '限时']];

  var notes = [];
  var shownShape = '';
  var hasReportedAccess = false;
  var hasReportedCards = false;
  var lastFailure = '';
  var setupData = null;
  var shopData = null;
  var isShopAsked = false;
  var bansData = null;
  var settingsData = null;
  var asked = { bans: false, settings: false };
  var rewardsDoc = null;
  var rewardsFrame = null;
  var shopView = { query: '', cat: 'all', life: 'all', onlyBuyable: false };

  // ---------- Pure helpers (also used by the tests) ----------

  // Rows of at most five, as even as they can be: 6 cards are 3 + 3, 7 are 4 + 3, 10 are 5 + 5, 11 are 4 + 4 + 3.
  function layout(cardCount) {
    if (cardCount <= GAME_CARDS) return null;

    var rows = Math.ceil(cardCount / MOST_PER_ROW);

    return { rows: rows, columns: Math.ceil(cardCount / rows) };
  }

  // What the points held now pay when the day ends: the rate, rounded down, at least 1 and at most the cap. The tiny
  // nudge keeps a product like 0.29 * 100 from landing just under a whole number.
  function interest(points, rate, cap) {
    if (points <= 0 || rate <= 0) return 0;

    return Math.max(1, Math.min(cap, Math.floor(points * rate + 1e-9)));
  }

  // What the shop lists for a search: the category chips and the how-long-it-lasts chips with their counts, and the
  // rows in the chosen category and kind, the ones that can be bought first, cheapest first. Each set of counts follows
  // the other filter, and a choice with nothing in it falls back to all. With no buys left today every row says so.
  function shopList(data, view, lang) {
    var query = (view.query || '').trim().toLowerCase();
    var noBuys = data.left === 0 ? say(TEXT.noBuys, lang) : null;
    var matching = data.rows.map(function (row) {
      return { row: row, life: lifeLabel(row.life, lang).kind, why: row.why || noBuys, isPoor: !row.why && !noBuys && row.price > data.points };
    }).filter(function (entry) {
      if (view.onlyBuyable && (entry.why || entry.isPoor)) return false;

      return !query || (entry.row.name + ' ' + entry.row.desc).toLowerCase().indexOf(query) >= 0;
    });
    var inLife = function (life) { return function (entry) { return life === 'all' || entry.life === life; }; };
    var inCat = function (cat) { return function (entry) { return cat === 'all' || entry.row.cat === cat; }; };
    var catCounts = tally(matching.filter(inLife(view.life || 'all')), 'cat');
    var cat = catCounts[view.cat] ? view.cat : 'all';
    var lifeCounts = tally(matching.filter(inCat(cat)), 'life');
    var life = lifeCounts[view.life] ? view.life : 'all';

    catCounts = tally(matching.filter(inLife(life)), 'cat');

    var rows = matching.filter(function (entry) { return inCat(cat)(entry) && inLife(life)(entry); });

    rows.sort(function (a, b) { return (a.why ? 1 : 0) - (b.why ? 1 : 0) || a.row.price - b.row.price; });

    return {
      cat: cat,
      cats: chipsOf(CATS, catCounts, lang),
      life: life,
      lives: chipsOf(LIVES, lifeCounts, lang),
      rows: rows
    };
  }

  function tally(entries, key) {
    var counts = { all: entries.length };

    entries.forEach(function (entry) {
      var value = key === 'cat' ? entry.row.cat : entry.life;

      counts[value] = (counts[value] || 0) + 1;
    });

    return counts;
  }

  function chipsOf(list, counts, lang) {
    return list.filter(function (c) { return counts[c[0]]; }).map(function (c) { return { key: c[0], label: lang === 'zh' ? c[2] : c[1], count: counts[c[0]] }; });
  }

  // A card's or shop row's label from how long its reward lasts, in game seconds: below 0 is permanent, 0 is one-time,
  // and anything else is how long, in whole days from 2 days up, otherwise in hours or minutes.
  function lifeLabel(seconds, lang) {
    var isZh = lang === 'zh';

    if (seconds === undefined || seconds === null || seconds < 0) return { kind: 'perm', text: say(TEXT.perm, lang) };

    if (seconds === 0) return { kind: 'once', text: say(TEXT.once, lang) };

    var count = Math.round(seconds / 60);
    var unit = isZh ? '分钟' : ' MIN';

    if (seconds >= 2 * 86400 && seconds % 86400 === 0) {
      count = seconds / 86400;
      unit = isZh ? '天' : ' DAYS';
    } else if (seconds % 3600 === 0) {
      count = seconds / 3600;
      unit = isZh ? '小时' : count === 1 ? ' HOUR' : ' HOURS';
    }

    return { kind: 'timed', text: isZh ? '持续' + count + unit : count + unit };
  }

  function say(text, lang) {
    return text[lang === 'zh' ? 1 : 0];
  }

  // ---------- Talking to the plugin ----------

  function post(message) {
    if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
  }

  function setup(data) {
    setupData = data;
  }

  function shop(data) {
    shopData = data;

    var doc = rewardsDoc;

    if (!doc || !(isShopAsked || isShopOpen(doc))) return;

    isShopAsked = false;
    drawShop(doc);
  }

  function bans(data) {
    bansData = data;
    drawIfWanted('bans', drawBans);
  }

  function settings(data) {
    settingsData = data;
    drawIfWanted('settings', drawSettings);
  }

  // A panel is drawn when it was just opened, or again with new data while it is open.
  function drawIfWanted(name, draw) {
    var doc = rewardsDoc;

    if (!doc || !doc.body) return;

    var panel = doc.body.querySelector('.mr-pane-' + name);

    if (!asked[name] && !(panel && !panel.hidden)) return;

    asked[name] = false;
    draw(doc);
  }

  function toast(text) {
    var doc = rewardsDoc;

    if (!doc || !doc.body) return;

    var note = el(doc, 'div', 'toast2', text);

    doc.body.appendChild(note);
    setTimeout(function () { note.remove(); }, TOAST_MS);
  }

  function lang() {
    return setupData ? setupData.lang : 'en';
  }

  function t(text) {
    return say(text, lang());
  }

  // ---------- The rewards page ----------

  // The page's own Vue state, where the cards and the points are. The production build of Vue keeps the root
  // component on the mount element as _vnode; a development build also has it on __vue_app__.
  function vueState(doc) {
    var app = doc.__slmrApp;

    if (!app || !app.isConnected) app = doc.__slmrApp = doc.getElementById('app');

    var component = app && app._vnode && app._vnode.component;

    if (component && component.setupState) return component.setupState;

    var vueApp = app && app.__vue_app__;

    return vueApp && vueApp._instance ? vueApp._instance.setupState : null;
  }

  // One of the game page's own elements, found once and kept on its document so the next check does not search for
  // it; searched again once the page has dropped it. (check() has already made sure the document is still shown.)
  function keptOnDoc(doc, key, selector) {
    var node = doc[key];

    if (node && node.isConnected) return node;

    node = doc[key] = doc.querySelector(selector);

    return node;
  }

  // The card slots, read off the grid's own children: the game's page draws them straight into the grid.
  function cardSlots(doc) {
    var grid = keptOnDoc(doc, '__slmrGrid', '.card-grid');
    var slots = [];

    if (!grid) return slots;

    var children = grid.children;

    for (var i = 0; i < children.length; i++) {
      if (children[i].classList.contains('card-slot')) slots.push(children[i]);
    }

    return slots;
  }

  function arrange(doc, slots) {
    var root = doc.documentElement;
    var cardCount = slots.length;
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

    var columns = String(shape.columns);

    if (root.style.getPropertyValue(COLUMNS) !== columns) root.style.setProperty(COLUMNS, columns);

    noteShape(cardCount, shape);
  }

  function noteShape(cardCount, shape) {
    var key = cardCount + ':' + shape.columns;

    if (key === shownShape) return;

    shownShape = key;
    notes.push('showing ' + cardCount + ' rewards in ' + shape.rows + (shape.rows === 1 ? ' row' : ' rows') + ' of up to ' + shape.columns);
  }

  // The page may reload and lose the style, so it is checked on every check, through the one kept on the document; it
  // is never added twice.
  function addStyle(doc) {
    var style = doc.__slmrStyle;

    if (style && style.isConnected) return;

    style = doc.getElementById(STYLE_ID);

    if (!style) {
      style = doc.createElement('style');
      style.id = STYLE_ID;
      style.textContent = LAYOUT_CSS.concat(MOD_CSS).join('\n');
      doc.head.appendChild(style);
    }

    doc.__slmrStyle = style;
  }

  // The mod's additions, once the plugin has said what to draw.
  function decorate(doc, slots) {
    var state = vueState(doc);

    if (!state || !state.cards) {
      if (!hasReportedCards) notes.push('cannot read the rewards page cards, the mod buttons on the cards are not shown');

      hasReportedCards = true;

      return;
    }

    addStyle(doc);
    listen(doc);

    var locked = 0;

    for (var c = 0; c < state.cards.length; c++) {
      if (state.cards[c] && state.cards[c].pinned) locked++;
    }

    for (var i = 0; i < slots.length; i++) {
      if (state.cards[i]) decorateCard(slots[i], state.cards[i], locked);
    }

    showLocks(doc, locked);
    decorateFooter(doc, state, locked);
  }

  // Rebuilt only when what the card shows changes (Vue keeps the slot and swaps its card after a refresh).
  function decorateCard(slot, card, locked) {
    var cardEl = slot.__slmrCard;

    if (!cardEl || cardEl.parentElement !== slot) cardEl = slot.__slmrCard = slot.querySelector('.card');

    if (!cardEl) return;

    var isOpen = card.state !== 'bought' && card.state !== 'locked' && !card.pinned;
    var key = [card.id, isOpen, lang(), setupData.labels, setupData.ban, setupData.banCost].join('|');

    if (cardEl.__slmrKey !== key) {
      cardEl.__slmrKey = key;
      removeAll(cardEl, '.mr-life');
      removeAll(cardEl, '.mr-acts');

      if (setupData.labels) addLife(cardEl, card);

      if (isOpen && setupData.ban) addActs(cardEl, card, slot);
    }

    if (setupData.maxLocked > 1) setTip(cardEl, card, locked);
  }

  function addLife(cardEl, card) {
    var label = lifeLabel(setupData.life ? setupData.life[card.id] : undefined, lang());

    cardEl.insertBefore(el(cardEl.ownerDocument, 'span', 'mr-life ' + label.kind, label.text), cardEl.firstChild);
  }

  function addActs(cardEl, card, slot) {
    var doc = cardEl.ownerDocument;
    var acts = el(doc, 'div', 'mr-acts');
    var ban = acts.appendChild(button(doc, 'ban', t(TEXT.ban), function () { post('ban|' + card.id + '|' + slotIndex(slot)); }));

    if (setupData.banCost > 0) ban.appendChild(el(doc, 'b', '', String(setupData.banCost)));

    cardEl.appendChild(acts);
  }

  // The game draws the lock button (and its tip) only on cards that are not bought or locked.
  function setTip(cardEl, card, locked) {
    if (card.state === 'bought' || card.state === 'locked') return;

    var tip = cardEl.__slmrTip;

    if (!tip || !tip.isConnected) tip = cardEl.__slmrTip = cardEl.querySelector('.pin-tip');

    var text = card.pinned ? t(TEXT.unlock) : locked < setupData.maxLocked ? t(TEXT.lock) + ' (' + locked + '/' + setupData.maxLocked + ')' : t(TEXT.full);

    if (tip && tip.textContent !== text) tip.textContent = text;
  }

  function showLocks(doc, locked) {
    var root = doc.documentElement;
    var isMany = setupData.maxLocked > 1;

    if (isMany && locked < setupData.maxLocked) root.classList.add(LOCKS_LEFT);
    else root.classList.remove(LOCKS_LEFT);

    if (isMany && locked >= setupData.maxLocked) root.classList.add(LOCKS_FULL);
    else root.classList.remove(LOCKS_FULL);
  }

  function decorateFooter(doc, state, locked) {
    var left = keptOnDoc(doc, '__slmrLeft', '.footer-left');
    var center = keptOnDoc(doc, '__slmrCenter', '.footer-center');
    var points = Number(state.ownPoints) || 0;

    if (left) {
      setText(left, 'pin-count', setupData.maxLocked > 1 ? LOCK + ' ' + locked + '/' + setupData.maxLocked : null);
      footLines(left, points);
    }

    if (center) footButtons(doc, center);
  }

  function footLines(left, points) {
    var foot = ownChild(left, '__slmrFoot', '.mr-foot') || (left.__slmrFoot = left.appendChild(el(left.ownerDocument, 'div', 'mr-foot')));
    var lines = [t(TEXT.interest) + interest(points, setupData.rate, setupData.cap)];

    if (setupData.cashOut > 0) lines.push(t(TEXT.cashOut) + points * setupData.cashOut);

    var key = lines.join('\n');

    if (foot.__slmrKey === key) return;

    foot.__slmrKey = key;
    removeAll(foot, 'small');
    lines.forEach(function (line) { foot.appendChild(el(left.ownerDocument, 'small', '', line)); });
  }

  // Shop (when it is on), the ban list (when bans are on) and the settings, in that order, drawn again only when one
  // of them comes or goes or the language changes.
  function footButtons(doc, center) {
    var box = ownChild(center, '__slmrButtons', '.mr-foot-btns') || (center.__slmrButtons = center.appendChild(el(doc, 'span', 'mr-foot-btns')));
    var key = [lang(), setupData.shop, setupData.ban].join('|');

    if (box.__slmrKey === key) return;

    box.__slmrKey = key;
    removeChildren(box);

    if (setupData.shop) {
      box.appendChild(button(doc, 'ghost-btn mr-shop-btn', t(TEXT.shop), function () {
        isShopAsked = true;
        post('shop');
      }));
    }

    if (setupData.ban) {
      box.appendChild(button(doc, 'ghost-btn mr-bans-btn', t(TEXT.bans), function () {
        asked.bans = true;
        post('bans');
      }));
    }

    box.appendChild(button(doc, 'ghost-btn mr-set-btn', t(TEXT.settings), function () {
      asked.settings = true;
      post('settings');
    }));
  }

  // One capture listener per page document, so it hears a click before the game's own handlers. With more than one
  // lock allowed the lock button is the mod's: the game would refuse a second lock. Leaving cashes out first when
  // cashing out is on; the game still gets the click.
  function listen(doc) {
    if (doc.__slmrListening) return;

    doc.__slmrListening = true;
    doc.addEventListener('click', function (e) {
      try {
        onPageClick(e);
      } catch (err) {
        notes.push('rewards page click failed: ' + err);
      }
    }, true);
  }

  function onPageClick(e) {
    if (!setupData || !e.target || !e.target.closest) return;

    var pin = e.target.closest('.pin-btn');

    if (pin && setupData.maxLocked > 1) {
      e.stopPropagation();
      e.preventDefault();
      pinClicked(pin);

      return;
    }

    if (setupData.cashOut > 0 && e.target.closest('.primary-btn') && e.target.closest('.footer-right')) post('cashout');
  }

  function pinClicked(pin) {
    var slot = pin.closest('.card-slot');
    var state = vueState(pin.ownerDocument);
    var index = slot ? slotIndex(slot) : -1;
    var card = state && state.cards ? state.cards[index] : null;

    if (card) post('pin|' + card.id + '|' + index);
  }

  function slotIndex(slot) {
    var slots = slot.ownerDocument.querySelectorAll('.card-slot');

    for (var i = 0; i < slots.length; i++) {
      if (slots[i] === slot) return i;
    }

    return -1;
  }

  // ---------- The shop ----------

  function isShopOpen(doc) {
    var panel = doc.body && doc.body.querySelector('.shop');

    return !!panel && !panel.hidden;
  }

  function drawShop(doc) {
    if (!shopData || !doc.body) return;

    addStyle(doc);

    var panel = doc.body.querySelector('.shop') || buildShop(doc);

    panel.hidden = false;
    drawShopHeader(panel);
    drawShopList(panel);
  }

  // The frame of the shop is built once; the header, list and points are drawn again whenever the plugin sends the
  // shop, and the list again on every keystroke.
  function buildShop(doc) {
    var panel = el(doc, 'div', 'shop');
    var card = panel.appendChild(el(doc, 'div', 'shop-card'));
    var head = card.appendChild(el(doc, 'div', 'shop-hd'));
    var tools = card.appendChild(el(doc, 'div', 'shop-tools'));
    var input = tools.appendChild(doc.createElement('input'));
    var lives = tools.appendChild(el(doc, 'div', 'shop-life chips'));
    var chips = tools.appendChild(el(doc, 'div', 'chips'));
    var only = chips.appendChild(el(doc, 'span', 'only-buyable'));

    head.appendChild(el(doc, 'h2'));
    head.appendChild(el(doc, 'div', 'rate'));
    head.appendChild(el(doc, 'span', 'sp'));
    head.appendChild(button(doc, 'x', '\u2715', function () { panel.hidden = true; }));
    card.appendChild(el(doc, 'div', 'shop-cats chips'));
    card.appendChild(el(doc, 'div', 'shop-list'));
    card.appendChild(el(doc, 'div', 'shop-ft'));

    input.value = shopView.query;
    input.addEventListener('input', function () {
      shopView.query = input.value;
      drawShopList(panel);
    });
    // Typed keys stay in the box: the game must not read them as hotkeys. Escape closes the shop.
    input.addEventListener('keydown', function (e) {
      e.stopPropagation();

      if (e.key === 'Escape') panel.hidden = true;
    });
    only.addEventListener('click', function () {
      shopView.onlyBuyable = !shopView.onlyBuyable;
      drawShopList(panel);
    });
    panel.addEventListener('click', function (e) {
      if (e.target === panel) panel.hidden = true;
    });
    doc.body.appendChild(panel);

    return panel;
  }

  function drawShopHeader(panel) {
    var doc = panel.ownerDocument;
    var rate = panel.querySelector('.rate');
    var foot = panel.querySelector('.shop-ft');

    panel.querySelector('h2').textContent = t(TEXT.shop);
    panel.querySelector('input').placeholder = t(TEXT.search);
    panel.querySelector('.only-buyable').textContent = t(TEXT.onlyBuyable);

    removeChildren(rate);
    rate.appendChild(el(doc, 'span', '', t(TEXT.pricesNow)));
    rate.appendChild(el(doc, 'b', '', '+' + shopData.rise + '%'));
    rate.appendChild(doc.createElement('br'));
    rate.appendChild(el(doc, 'span', '', t(TEXT.eachBuy)));
    rate.appendChild(el(doc, 'b', '', setupData ? setupData.shopIncrease + '%' : ''));
    rate.appendChild(el(doc, 'span', '', ', ' + t(TEXT[setupData ? setupData.shopReset : 'day'] || TEXT.day)));

    if (shopData.left !== null && shopData.left !== undefined) {
      rate.appendChild(el(doc, 'span', '', DOT + t(TEXT.left)));
      rate.appendChild(el(doc, 'b', '', String(shopData.left)));
    }

    removeChildren(foot);
    foot.appendChild(el(doc, 'span', '', t(TEXT.own)));
    foot.appendChild(el(doc, 'b', '', String(shopData.points)));
    foot.appendChild(el(doc, 'span', 'sp'));
    foot.appendChild(el(doc, 'span', '', t(TEXT.shopNote)));
  }

  function drawShopList(panel) {
    var doc = panel.ownerDocument;
    var view = shopList(shopData, shopView, lang());
    var cats = panel.querySelector('.shop-cats');
    var list = panel.querySelector('.shop-list');

    shopView.cat = view.cat;
    shopView.life = view.life;
    drawChips(panel, panel.querySelector('.shop-life'), view.lives, view.life, 'life');
    panel.querySelector('.only-buyable').className = 'only-buyable' + (shopView.onlyBuyable ? ' on' : '');
    drawChips(panel, cats, view.cats, view.cat, 'cat');
    removeChildren(list);

    if (!view.rows.length) list.appendChild(el(doc, 'div', 'shop-none', t(TEXT.none)));

    view.rows.forEach(function (entry) { list.appendChild(shopRow(doc, entry)); });
  }

  // One row of filter chips, each with its count; clicking one sets that filter and draws the list again.
  function drawChips(panel, box, chips, chosen, key) {
    var doc = panel.ownerDocument;

    removeChildren(box);
    chips.forEach(function (chipData) {
      var chip = box.appendChild(el(doc, 'span', chipData.key === chosen ? 'on' : '', chipData.label));

      chip.appendChild(el(doc, 'i', '', String(chipData.count)));
      chip.addEventListener('click', function () {
        shopView[key] = chipData.key;
        drawShopList(panel);
      });
    });
  }

  function shopRow(doc, entry) {
    var row = entry.row;
    var line = el(doc, 'div', 'sr' + (entry.why ? ' grey' : '') + (entry.isPoor ? ' poor' : ''));
    var text = el(doc, 'div', 'txt');
    var name = text.appendChild(el(doc, 'div', 'n', row.name));
    var buy = el(doc, 'div', 'buy');
    var price = buy.appendChild(el(doc, 'div', 'p'));

    if (row.icon) {
      var icon = line.appendChild(doc.createElement('img'));

      icon.src = row.icon;
    } else {
      line.appendChild(el(doc, 'span'));
    }

    if (setupData && setupData.labels) {
      var label = lifeLabel(row.life, lang());

      name.appendChild(el(doc, 'span', 'tag ' + label.kind, label.text));
    }

    text.appendChild(el(doc, 'div', 'd', row.desc));

    if (entry.why) text.appendChild(el(doc, 'div', 'why', entry.why));
    else if (row.early) text.appendChild(el(doc, 'div', 'early', t(TEXT.early)));

    price.appendChild(el(doc, 's', '', String(row.cost)));
    price.appendChild(el(doc, 'span', '', String(row.price)));

    if (!entry.why && !entry.isPoor) buy.appendChild(button(doc, '', t(TEXT.buy), function () { post('shopbuy|' + row.id); }));

    line.appendChild(text);
    line.appendChild(buy);

    return line;
  }

  // ---------- The ban list and the settings ----------

  // The frame both panels share, built once: a title, a note, and a list drawn again with each answer from the plugin.
  function pane(doc, name) {
    var panel = doc.body.querySelector('.mr-pane-' + name);

    if (panel) return panel;

    panel = el(doc, 'div', 'mr-pane mr-pane-' + name);

    var card = panel.appendChild(el(doc, 'div', 'mr-pane-card'));
    var head = card.appendChild(el(doc, 'div', 'mr-pane-hd'));

    head.appendChild(el(doc, 'h2'));
    head.appendChild(el(doc, 'span', 'sp'));
    head.appendChild(button(doc, 'x', '\u2715', function () { panel.hidden = true; }));
    card.appendChild(el(doc, 'div', 'mr-pane-note'));
    card.appendChild(el(doc, 'div', 'mr-pane-list'));
    panel.addEventListener('click', function (e) {
      if (e.target === panel) panel.hidden = true;
    });
    doc.body.appendChild(panel);

    return panel;
  }

  // Opens the panel with its title and note, and hands back its list, emptied, keeping how far it was scrolled.
  function openPane(doc, name, title, note) {
    addStyle(doc);

    var panel = pane(doc, name);
    var list = panel.querySelector('.mr-pane-list');
    var scrolled = list.scrollTop || 0;

    panel.hidden = false;
    panel.querySelector('h2').textContent = title;
    panel.querySelector('.mr-pane-note').textContent = note;
    removeChildren(list);
    setTimeout(function () { list.scrollTop = scrolled; });

    return list;
  }

  function drawBans(doc) {
    if (!bansData) return;

    var list = openPane(doc, 'bans', t(TEXT.bans), t(TEXT.bansNote) + bansData.refund + t(TEXT.bansNoteEnd));

    if (!bansData.rows.length) list.appendChild(el(doc, 'div', 'mr-pane-none', t(TEXT.noBans)));

    bansData.rows.forEach(function (row) {
      var line = list.appendChild(el(doc, 'div', 'mr-row mr-ban'));

      line.appendChild(el(doc, 'div', 'txt')).appendChild(el(doc, 'div', 'n', row.names.join(lang() === 'zh' ? '、' : ', ')));

      if (row.refund > 0) line.appendChild(el(doc, 'span', 'r', '+' + row.refund));

      line.appendChild(button(doc, '', t(TEXT.remove), function () { post('unban|' + row.id); }));
    });
  }

  function drawSettings(doc) {
    if (!settingsData) return;

    var list = openPane(doc, 'settings', t(TEXT.settings), t(TEXT.settingsNote));
    var section = '';

    settingsData.rows.forEach(function (row) {
      if (!row.shown) return;

      if (row.section !== section) {
        section = row.section;
        list.appendChild(el(doc, 'h3', '', lang() === 'zh' && SECTIONS[section] ? SECTIONS[section] : section));
      }

      list.appendChild(settingRow(doc, row));
    });
  }

  function settingRow(doc, row) {
    var line = el(doc, 'div', 'mr-row mr-setting');
    var text = line.appendChild(el(doc, 'div', 'txt'));
    var name = text.appendChild(el(doc, 'div', 'n', row.name));
    var controls = line.appendChild(el(doc, 'div', 'ctl'));

    if (row.restart) name.appendChild(el(doc, 'span', 'rs', t(TEXT.restart)));

    text.appendChild(el(doc, 'div', 'd', row.desc));

    if (row.kind === 'bool') {
      var isOn = row.value === 'true';

      controls.appendChild(button(doc, isOn ? 'on' : 'off', t(isOn ? TEXT.on : TEXT.off), function () { setSetting(row, isOn ? 'false' : 'true'); }));
    } else if (row.kind === 'choice') {
      var chips = controls.appendChild(el(doc, 'div', 'chips'));

      row.options.forEach(function (option) {
        var chip = chips.appendChild(el(doc, 'span', option === row.value ? 'on' : '', option));

        chip.addEventListener('click', function () { setSetting(row, option); });
      });
    } else {
      numberControls(doc, controls, row);
    }

    return line;
  }

  // A number box between - and +. A typed number is sent when the box loses focus or on Enter, held to the range.
  function numberControls(doc, controls, row) {
    var value = Number(row.value) || 0;
    var input = doc.createElement('input');
    var send = function (number) {
      if (isNaN(number)) return;

      var held = Math.max(row.min, Math.min(row.max, Math.round(number)));

      if (String(held) !== row.value) setSetting(row, String(held));
    };

    controls.appendChild(button(doc, '', '-', function () { send(value - 1); }));
    controls.appendChild(input);
    controls.appendChild(button(doc, '', '+', function () { send(value + 1); }));
    input.value = row.value;
    input.addEventListener('change', function () { send(Number(input.value)); });
    // Typed keys stay in the box: the game must not read them as hotkeys.
    input.addEventListener('keydown', function (e) {
      e.stopPropagation();

      if (e.key === 'Enter') send(Number(input.value));

      if (e.key === 'Escape') input.closest('.mr-pane').hidden = true;
    });
  }

  function setSetting(row, value) {
    post('set|' + row.key + '|' + value);
  }

  // ---------- Small DOM helpers ----------

  function el(doc, tag, className, text) {
    var node = doc.createElement(tag);

    if (className) node.className = className;

    if (text !== undefined) node.textContent = text;

    return node;
  }

  // The mod's buttons keep their clicks to themselves, so a click never reaches the card's own handlers.
  function button(doc, className, text, onClick) {
    var node = el(doc, 'button', className, text);

    node.addEventListener('click', function (e) {
      e.stopPropagation();
      onClick();
    });

    return node;
  }

  function setText(parent, className, text) {
    var key = '__slmr_' + className;
    var node = ownChild(parent, key, '.' + className);

    if (text === null) {
      if (node) node.remove();

      parent[key] = null;

      return;
    }

    if (!node) node = parent[key] = parent.appendChild(el(parent.ownerDocument, 'span', className));

    if (node.textContent !== text) node.textContent = text;
  }

  // One of the mod's own nodes straight under parent, kept on parent so later checks do not search for it. Only the
  // mod adds these nodes, and it keeps what it kept up to date when it adds or removes one, so "there is none" is kept
  // too; it is searched again only once the game's page has moved or dropped it.
  function ownChild(parent, key, selector) {
    var node = parent[key];

    if (node === null || (node && node.parentElement === parent)) return node;

    node = parent[key] = parent.querySelector(selector);

    return node;
  }

  function removeAll(parent, selector) {
    var found = parent.querySelectorAll(selector);

    for (var i = 0; i < found.length; i++) found[i].remove();
  }

  function removeChildren(parent) {
    while (parent.firstChild) parent.removeChild(parent.firstChild);
  }

  // ---------- The check ----------

  // The rewards page is a same-origin iframe, so its document can be reached from here; polled because the game
  // creates and drops that iframe whenever it likes. The frame found is kept while it still shows the same page, so a
  // check with the page up searches nothing.
  function check() {
    if (!isStillShown(rewardsFrame, rewardsDoc)) findRewards();

    if (!rewardsDoc) return;

    var slots = cardSlots(rewardsDoc);

    arrange(rewardsDoc, slots);

    if (setupData) decorate(rewardsDoc, slots);
  }

  // A document kept from inside an iframe still answers after the game removes the iframe or loads another page into
  // it, so it is checked through its frame.
  function isStillShown(frame, doc) {
    if (!frame || !frame.isConnected || (frame.src || '').indexOf(REWARDS_PAGE) < 0) return false;

    try {
      return frame.contentDocument === doc && !!doc.documentElement;
    } catch (e) {
      return false;
    }
  }

  function findRewards() {
    var frames = document.querySelectorAll('iframe');

    rewardsFrame = null;
    rewardsDoc = null;

    for (var i = 0; i < frames.length; i++) {
      if ((frames[i].src || '').indexOf(REWARDS_PAGE) < 0) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc || !doc.documentElement) {
        if (!hasReportedAccess) notes.push('cannot reach the rewards page document (root ' + location.protocol + ', iframe ' + frames[i].src + ')');

        hasReportedAccess = true;

        continue;
      }

      rewardsFrame = frames[i];
      rewardsDoc = doc;
    }
  }

  // The plugin's check. "needs setup" asks for what to draw while the rewards page is up and the page has not been told.
  function take() {
    if (rewardsDoc && !setupData) notes.push('needs setup');

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

  window.__slmorerewards = { take: take, check: check, layout: layout, setup: setup, shop: shop, bans: bans, settings: settings, toast: toast, interest: interest, shopList: shopList, lifeLabel: lifeLabel };
})();
