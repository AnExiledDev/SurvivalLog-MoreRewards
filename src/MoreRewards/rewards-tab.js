// The Rewards tab in the Mod Manager: what each end-of-day reward costs, how many levels it has, what it gives, the
// pools it shows up in and the text the player reads, saved to MoreRewards.Rewards.json. Runs in Root.html; the plugin
// sends it again whenever the page reloads. Talks to the plugin through
// vuplex.postMessage("slmod|morerewards|rewards|...") and is answered through window.__slmrRewards.
(function () {
  if (window.__slmrRewards) return;
  if (!window.ExiledEditor || window.ExiledEditor.version < 1) return;

  // The shared editor from Exiled's Toolkit. It reads this tab's side from T, filled in below.
  const T = {};
  const {
    FX, FX_BY_ID, GAME_PREFIX, L, MAX_GIVES, MAX_LEVELS, NO_SCRIPT, REWARD_ICONS, TOAST_MS, VARS, adopt, afterRemove,
    autoDesc, autoLevelText, availableOptions, baseText, cat, choose, chooseButton, clampLevel, clone, closeIcons,
    closeOverlays, closePicker, closeToast, defaultAt, descOf, descTarget, discard, display, editorHtml, entryOf,
    entryOrGhost, esc, escapeTab, fieldName, fixedFrom, fmt, fromRoot, fx, fxIcon, fxName, givesChanged, h,
    hasLevelDescs, hasNoScript, hideTab, hidesOf, iconsHtml, injectStyle, isArr, isAutoOn, isEditable, isNum, isObj,
    levelCount, listIds, markChanged, match, matchesQuery, nameOf, nameOptions, nextStep, normText, numText, numsOf,
    onDocDown, onInput, onPickerDouble, onPickerKey, paramRow, pct, pickHtml, picksOf, range, rawCache, rawFx,
    readsHtml, renderPicker, resetOrDelete, round2, roundTrips, saved, scriptOfGive, select, send, setPick, setText,
    showIconGroup, stepper, tag, talentIcons, talentName, textIn, toRaw, unitOf, winEl, writeText, editorCss,
    searchHtml, noMatchHtml, clearSearch, onSearchKey, showSelectedRow, fitHeight, watchHeight,
    renderAfterChange, watchPresses, refreshSaved, keepSelectionInView
  } = window.ExiledEditor.make(T);

  const PREFIX = 'slmod|morerewards|rewards|';
  const GUID = 'com.dev.survivallog.morerewards';
  const TAB_ID = 'rewards';
  const TITLE = { en: 'Rewards', zh: '奖励' };
  const FILE_NAME = 'MoreRewards.Rewards.json';
  const STYLE_ID = 'slmr-rewards-css';
  // New rewards take IDs from here; 39001 to 39003 are the wind rewards.
  const FIRST_NEW = 39100;
  const LAST_ID = 39999;
  const MR_PREFIX = 'mr:';

  // ===== Style =====

  // Each line starts with who it is for: N the tab, P the effect picker, B both, I the icon picker, T the toast, D the
  // pool picker. "&" is the element itself; anything else sits inside it.
  const SCOPES = {
    N: ['#slmm .mr'],
    P: ['#slmm .mr-fxp'],
    B: ['#slmm .mr', '#slmm .mr-fxp'],
    I: ['#slmm .mr-icons'],
    T: ['#slmm .mr-toast'],
    D: ['#slmm .mr-pools']
  };

  const CSS = editorCss("mr", [
    'N .pl{border:1px solid var(--bd2);background:rgba(0,0,0,0.22);padding:6px 12px}',
    'N .pl-row{display:grid;grid-template-columns:minmax(0,1fr) auto 24px;gap:12px;align-items:center;padding:4px 0;border-bottom:1px solid rgba(255,255,255,0.05)}',
    'N .pl-id{color:#ddd;font-size:.92rem}',
    'N .pl-id .tg{margin-left:8px}',
    'N .pl-id{min-width:0}',
    'N .pl-id small{display:block;color:var(--td);font-size:.8rem;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    'N .pl-row .x{cursor:pointer;color:var(--td)}',
    'N .pl-add{display:flex;flex-wrap:wrap;gap:12px;align-items:center;padding:8px 0 4px}',
    'N .pk-btn{padding:6px 12px;border:1px dashed var(--hl);color:var(--hl);cursor:pointer;letter-spacing:2px;font-size:.82rem;font-weight:700;white-space:nowrap}',
    'N .pk-btn:hover,.pk-btn.is-open{background:rgba(255,200,58,0.12);color:#fff}',
    'N .chs{margin:0;flex-wrap:wrap}',
    'N .chs span{min-width:0;padding:6px 12px;font-family:var(--body)}',
    'N .ro{color:var(--td);font-size:.85rem;line-height:1.4}',
    'N .dy{display:flex;justify-content:space-between;align-items:center;gap:8px;padding:9px 14px;cursor:pointer;color:var(--tm);border-bottom:1px solid rgba(255,255,255,0.04)}',
    'N .dy:hover{background:rgba(255,200,58,0.05)}',
    'N .dy.is-sel{background:rgba(255,200,58,0.12);color:#fff}',
    'N .dy small{color:var(--td);font-family:var(--mono)}',
    'N .dpl{grid-template-columns:minmax(0,1fr) 54px auto 24px}',
    'N .dpl .pl-id{cursor:pointer}',
    'N .dpl .pl-id small{display:inline;margin-left:8px}',
    'N .mv b{display:inline-block;width:24px;text-align:center;color:var(--hl);cursor:pointer;user-select:none}',
    'N .pm{margin:2px 0 8px 18px;border-left:1px solid var(--bd2);padding-left:10px}',
    'N .pm div{display:grid;grid-template-columns:minmax(0,1fr) 70px 60px;gap:10px;padding:3px 0;font-size:.86rem;color:var(--tm)}',
    'N .pm span{text-align:right;font-family:var(--mono);color:var(--td)}',
    'N .drw.is-cut{opacity:.5}',
    'N .drw .nm2{text-align:right}',
    'N .drw-n{display:flex;align-items:center;gap:8px;min-width:0}',
    'N .drw-n img{width:28px;height:28px;object-fit:contain;flex:none}',
    'N .nm2{color:var(--td);font-size:.82rem;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    'N .tx{grid-template-columns:180px minmax(0,1fr) minmax(0,1fr)}',
    'N .chw{grid-column:span 2;display:flex;flex-wrap:wrap;align-items:center;gap:6px 14px}',
    'N .tx-n{grid-column:span 2;margin-top:-4px;color:var(--td);font-size:.8rem}',
    'D &{' + VARS + 'position:absolute;z-index:60;width:500px;max-width:calc(100% - 20px);box-sizing:border-box;background:#1a1611;border:1px solid rgba(255,200,58,0.4);box-shadow:0 20px 60px rgba(0,0,0,0.85);color:var(--tm)}',
    'D ::-webkit-scrollbar{width:6px}',
    'D ::-webkit-scrollbar-thumb{background:rgba(255,200,58,0.3)}',
    'D .pq{display:flex;align-items:center;gap:8px;margin:10px;padding:0 10px;border:1px solid var(--bl);background:rgba(0,0,0,0.45);color:var(--tmeta)}',
    'D .pq:focus-within{border-color:var(--hl)}',
    'D .pq input{flex:1;min-width:0;background:transparent;border:0;outline:none;color:#fff;font:400 .95rem var(--body);padding:7px 0;user-select:text}',
    'D .pq input::placeholder{color:#6d6253}',
    'D .po-list{max-height:390px;overflow-y:auto;border-top:1px solid var(--bd2)}',
    'D .po-sec{position:sticky;top:0;z-index:1;padding:7px 14px;font-size:.7rem;letter-spacing:2px;color:var(--hl);background:#1d1813;border-bottom:1px solid var(--bd2)}',
    'D .po-sec small{display:block;letter-spacing:0;color:var(--tmeta);font-size:.75rem}',
    'D .po{padding:8px 14px;cursor:pointer;border-left:3px solid transparent;border-bottom:1px solid rgba(255,255,255,0.04)}',
    'D .po:hover{background:rgba(255,200,58,0.05)}',
    'D .po.is-at{background:rgba(255,200,58,0.11);border-left-color:var(--hl)}',
    'D .po-h{display:flex;align-items:baseline;flex-wrap:wrap;gap:4px 12px}',
    'D .po-h b{color:#fff;font-size:.98rem}',
    'D .po-h span{color:var(--td);font-size:.8rem}',
    'D .po.is-empty .po-h b{color:var(--td)}',
    'D .po-r{display:flex;align-items:center;gap:6px;margin-top:5px;font-size:.8rem;color:var(--td);min-width:0}',
    'D .po-r img{width:24px;height:24px;object-fit:contain;flex:none}',
    'D .po-r span{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    'D .po-none{padding:18px;text-align:center;color:var(--td)}',
    'D .po-ft{padding:7px 14px;font-size:.75rem;color:var(--tmeta);border-top:1px solid var(--bd2)}'
  ]);

  // ===== State =====

  const st = {
    data: null,
    file: null,
    game: [],
    byId: {},
    scripts: new Set(),
    templates: {},
    allPools: [],
    builtIn: {},
    entries: [],
    ghosts: {},
    sel: 0,
    lv: 0,
    view: 'file',
    lang: 'en',
    pvLang: 'en',
    dirty: false,
    root: null,
    ctx: null,
    asked: false,
    picker: null,
    pickerEl: null,
    iconsEl: null,
    icons: [],
    iconGroup: REWARD_ICONS,
    toastEl: null,
    toastTimer: 0,
    pending: null,
    textOpen: false,
    listScroll: 0,
    problems: [],
    characters: [],
    gameDays: [],
    dayEdits: {},
    day: 0,
    peek: 0,
    drawn: null,
    q: '',
    menu: null,
    menuEl: null
  };

  // ===== Small helpers =====

  // ===== What a reward can give =====

  // ---- The game's effect scripts, with names and units a player can read. ----

  // ---- Game scripts only the game's in-run talents use. ----

  // ---- More Rewards: effects it runs itself, written to the file as "mr:<name>". ----

  // Offered only with Wind Power installed; it multiplies Wind Power's turbines.
  fx({ id: 'mr:wind-power', src: 'mr', mr: 'wind-power', needs: 'windPower', cat: 'home', scripts: [],
    en: 'Better wind turbines', zh: '风力增效', what: ['Wind turbines make more power. Needs Wind Power.', '风力发电机产电更多。需要Wind Power模组。'],
    params: [pct('FloatValue0', 'More power', '产电增加', [15, 30, 45])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '风力发电机产电提高' + fmt(v.FloatValue0) + '%' : 'Wind turbine output increased by ' + fmt(v.FloatValue0) + '%.' });

  FX.forEach(f => { FX_BY_ID[f.id] = f; });

  // ---- Params of an effect, and what a number in the file reads as. ----

  const isAvailable = f => f.needs ? !!(st.data && st.data[f.needs]) : !!f.mr || f.scripts.some(s => st.scripts.has(s));

  // ---- Which effect a script and its numbers are. ----

  // ---- The whole catalogue, built once the game's data is in. ----

  function buildCatalogue() {
    Object.keys(rawCache).forEach(k => { delete rawCache[k]; });
    FX.forEach(f => { f.usedBy = []; });
    nameOptions();

    cat.icons = st.icons.length ? st.icons : talentIcons();
    cat.defaultIcon = cat.icons[0] || { icon: '', unityIcon: '' };

    st.game.forEach(t => {
      const first = t.levels[0];

      if (!first || NO_SCRIPT.indexOf(first.script) >= 0 || !st.scripts.has(first.script)) return;

      match(first.script, first.values).usedBy.push(t.id);
    });

    const covered = new Set();

    FX.filter(f => f.pickable && isAvailable(f)).forEach(f => f.scripts.forEach(s => covered.add(s)));
    st.scripts.forEach(s => { if (NO_SCRIPT.indexOf(s) < 0 && !covered.has(s)) rawFx(s); });

    cat.list = FX.filter(f => f.pickable && isAvailable(f)).concat(Object.keys(rawCache).sort().map(k => rawCache[k]).filter(f => !covered.has(f.script)));
  }

  // ===== Gives: one effect of a reward, with its picks and a number per level =====

  // The lowest effect row in the game that runs this script, which the plugin copies for every give.
  function firstTpl(script) {
    return script && isObj(st.templates[script]) ? st.templates[script] : {};
  }

  // Every give starts from the same row, whichever reward and level it is on.
  const tplValues = script => firstTpl(script);

  function makeGive(f, script, tid, levelValues, count) {
    const has = isArr(levelValues) && levelValues.length > 0;
    const v0 = has ? levelValues[0] : null;
    const g = { fx: f, tid, script: '', pick: {}, per: {}, fixed: {}, field: '' };

    picksOf(f).forEach(p => {
      const options = availableOptions(p);

      if (v0 && p.virtual) g.pick[p.key] = p.from(v0, script);
      else if (v0 && p.key in v0) g.pick[p.key] = v0[p.key];
      else g.pick[p.key] = options.some(o => o.v === p.def) || !options.length ? p.def : options[0].v;
    });

    g.script = f.mr ? '' : f.scriptOf ? f.scriptOf(g) : (script || f.scripts[0]);

    if (f.init) f.init(g, tplValues(g.script, tid, 1));

    numsOf(f).forEach(p => {
      g.per[p.key] = range(count).map(i => has ? display(p, levelValues[Math.min(i, levelValues.length - 1)][fieldName(g, p)]) : defaultAt(p, i));
    });
    g.fixed = v0 ? fixedFrom(g, v0) : {};

    return g;
  }

  function writeGive(g, count) {
    const f = g.fx;
    const choice = clone(g.fixed) || {};
    const nums = numsOf(f);
    const out = { effect: f.mr ? MR_PREFIX + f.mr : GAME_PREFIX + g.script };

    hidesOf(f).forEach(p => { choice[p.key] = p.def; });
    picksOf(f).filter(p => !p.virtual).forEach(p => { choice[p.key] = g.pick[p.key]; });

    if (f.zeroOthers) {
      [tplValues(g.script, g.tid, 1), firstTpl(g.script)].forEach(t => Object.keys(t).forEach(k => {
        if (!(k in choice) && isNum(t[k]) && t[k] !== 0 && f.zeroOthers(k, g)) choice[k] = 0;
      }));
    }

    if (Object.keys(choice).length) out.fields = choice;

    if (nums.length) {
      out.levels = range(count).map(i => {
        const level = {};

        nums.forEach(p => {
          const field = fieldName(g, p);

          level[field] = toRaw(p, field, g.per[p.key][i]);
        });

        return level;
      });
    }

    return out;
  }

  // The effect row a give in the file ends up as at level i, the way the plugin builds it.
  function effectValues(give, tid, i) {
    const levels = isArr(give.levels) ? give.levels : [];
    const level = levels.length ? levels[Math.min(i, levels.length - 1)] : {};

    return Object.assign({}, tplValues(scriptOfGive(give), tid, i + 1), isObj(give.fields) ? give.fields : {}, isObj(level) ? level : {});
  }

  function giveFor(fileGive, tid, count) {
    if (!isObj(fileGive) || typeof fileGive.effect !== 'string') return null;

    if (fileGive.effect.indexOf(MR_PREFIX) === 0) return mrGiveFor(fileGive, tid, count);

    if (fileGive.effect.indexOf(GAME_PREFIX) !== 0) return null;

    const script = scriptOfGive(fileGive);

    if (!st.scripts.has(script)) return null;

    const vals = range(count).map(i => effectValues(fileGive, tid, i));

    for (const f of [match(script, vals[0]), rawFx(script)]) {
      const g = makeGive(f, script, tid, vals, count);

      if (roundTrips(g, vals, count)) return g;
    }

    return null;
  }

  function mrGiveFor(fileGive, tid, count) {
    const name = fileGive.effect.slice(MR_PREFIX.length);
    const f = FX.find(x => x.mr === name && isAvailable(x));

    if (!f) return null;

    const vals = range(count).map(i => effectValues(fileGive, tid, i));
    const g = makeGive(f, '', tid, vals, count);

    return roundTrips(g, vals, count) ? g : null;
  }

  // What a game reward gives as it stands. Shown locked when the tab could not write it back the same way.
  function baseGives(e, base, count) {
    const levels = base.levels;
    const script = levels.length ? levels[0].script : '';

    if (NO_SCRIPT.indexOf(script) >= 0) return [];

    const vals = levels.map(l => l.values);
    const g = makeGive(match(script, vals[0]), script, e.id, vals, count);
    const isMixed = levels.some(l => l.script !== script);

    if (!st.scripts.has(script) || isMixed || !roundTrips(g, vals, count)) e.givesLocked = true;

    return [g];
  }

  // ===== Pools: { "pool ID": weight } =====

  const ROMAN = ['I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X'];

  function poolsFrom(obj) {
    const out = {};

    Object.keys(obj).forEach(k => {
      const id = Number(k);
      const w = obj[k];

      if (Number.isInteger(id) && id > 0 && isNum(w)) out[id] = Math.max(0, Math.round(w));
    });

    return out;
  }

  // The pools a level is in: its own when set, else the game's.
  const poolsOf = l => l.pools ? Object.assign({}, l.gamePools, l.pools) : l.gamePools || {};

  // A level's pools as the file writes them: only the ones that differ from the game's.
  function poolsOut(pools) {
    const out = {};

    Object.keys(pools).forEach(k => { out[String(k)] = pools[k]; });

    return out;
  }

  // ===== Entries: the rewards in the file, as the tab edits them =====

  // A reward made from scratch starts with the game's defaults: any run, every character, from the first day.
  const NO_RULES = { isReget: false, endless: 0, chars: [], layers: 0, fromDay: 0 };

  function entryFromRaw(raw, rawIndex) {
    const id = raw.id;
    const game = st.byId[id] || null;
    const copySrc = isNum(raw.copyFrom) ? st.byId[raw.copyFrom] || null : null;
    const fields = isObj(raw.fields) ? raw.fields : {};
    const fresh = !game && isArr(raw.gives) && typeof fields.WebIcon === 'string';
    const kind = game ? 'changed' : st.builtIn[id] ? 'builtIn' : copySrc || fresh ? 'new' : 'unknown';
    const base = fresh ? null : (game || copySrc);
    const rawLevels = isArr(raw.levels) ? raw.levels : null;
    const baseCount = base ? base.levels.length : 0;
    const count = rawLevels ? rawLevels.length : baseCount;
    const e = {
      id, kind, fresh, game, base, copySrc, raw, rawIndex,
      enabled: raw.enabled !== false,
      copyFrom: raw.copyFrom,
      name: normText(raw.name),
      description: normText(raw.description),
      needs: isArr(raw.needs) ? raw.needs.filter(isNum) : null,
      furniture: isArr(raw.furniture) ? raw.furniture.filter(isNum) : null,
      icon: fresh ? fromRoot(fields.WebIcon) : base ? base.icon : cat.defaultIcon.icon,
      unityIcon: typeof fields.Icon === 'string' ? fields.Icon : base ? base.unityIcon : cat.defaultIcon.unityIcon,
      levels: [],
      gives: [],
      givesLocked: false,
      givesTouched: false,
      frozen: false,
      autoText: false,
      autoManual: false,
      touched: false,
      removed: false
    };

    e.gameRules = base ? base.rules : NO_RULES;
    e.rules = {
      isReget: typeof fields.IsReget === 'boolean' ? fields.IsReget : e.gameRules.isReget,
      endless: isNum(fields.EndlessMode) ? fields.EndlessMode : e.gameRules.endless,
      chars: isArr(fields.PlayerSelectId) ? fields.PlayerSelectId.filter(isNum) : e.gameRules.chars.slice(),
      layers: isNum(raw.layers) ? raw.layers : base ? e.gameRules.layers : 1,
      fromDay: isNum(raw.fromDay) ? raw.fromDay : e.gameRules.fromDay
    };

    e.levels = range(count).map(i => {
      const rl = rawLevels && isObj(rawLevels[i]) ? rawLevels[i] : {};
      const bl = baseCount ? base.levels[Math.min(i, baseCount - 1)] : null;

      const ownPools = isObj(rl.pools) ? rl.pools : isObj(raw.pools) ? raw.pools : null;

      return {
        cost: isNum(rl.cost) ? rl.cost : isNum(raw.cost) ? raw.cost : bl ? bl.cost : 0,
        gameCost: bl && i < baseCount ? bl.cost : null,
        name: normText(rl.name),
        gameName: bl && i < baseCount && game ? bl.name : null,
        desc: normText(rl.description),
        gameDesc: bl ? bl.desc : null,
        pools: ownPools ? poolsFrom(ownPools) : null,
        gamePools: bl && i < baseCount && game ? bl.pools : {},
        isNew: !game || i >= baseCount,
        raw: rawLevels ? rl : null
      };
    });

    const hasExtras = ['effects', 'buffs', 'give'].some(k => k in raw) || e.levels.some(l => l.raw && isObj(l.raw.fields) && Object.keys(l.raw.fields).length > 0);

    if (kind === 'unknown' || count === 0 || count > MAX_LEVELS || hasExtras) e.frozen = true;

    if (!e.frozen) {
      if (isArr(raw.gives)) e.gives = raw.gives.map(gv => giveFor(gv, id, count));
      else if (base) e.gives = baseGives(e, base, count);
    }

    if (e.gives.some(g => !g)) {
      e.frozen = true;
      e.gives = [];
    }

    e.autoText = e.gives.length > 0 && e.levels.every((l, i) => !!l.desc && l.desc.en === autoDesc(e, i, false) && l.desc.zh === autoDesc(e, i, true));

    return e;
  }

  const canEditGives = e => isEditable(e) && !e.givesLocked;

  // ===== The text the player reads =====

  // The short line of numbers under the name: what each level gives, as the tab works it out.
  function levelLineOf(e, lang) {
    return e.gives.length ? autoLevelText(e, lang === 'zh') : '';
  }

  // A level's name: its own, else the game's, else the reward's name with the level's number, as the plugin writes it.
  function levelNameOf(e, i, lang) {
    const l = e.levels[i];
    const own = l ? textIn(l.name, lang) : '';

    return own || defaultLevelName(e, i, lang);
  }

  function defaultLevelName(e, i, lang) {
    const l = e.levels[i];
    const game = l && !e.name ? textIn(l.gameName, lang) : '';

    if (game) return game;

    const name = nameOf(e, lang);

    return e.levels.length === 1 ? name : lang === 'zh' ? name + ' Lv' + (i + 1) : name + ' ' + ROMAN[i];
  }

  // ===== Building and changing the list =====

  // The file's days as the tab edits them: day to [{ pool, amount }], in the order the game draws them.
  function daysFromFile(file) {
    const out = {};

    (file && isArr(file.days) ? file.days : []).forEach(x => {
      if (isObj(x) && isNum(x.day) && isArr(x.pools)) out[x.day] = x.pools.filter(p => isObj(p) && isNum(p.pool) && isNum(p.amount)).map(p => ({ pool: p.pool, amount: p.amount }));
    });

    return out;
  }

  function buildEntries(keepId) {
    const rewards = st.file && isArr(st.file.rewards) ? st.file.rewards : [];

    st.entries = [];
    st.ghosts = {};
    st.dayEdits = daysFromFile(st.file);
    st.drawn = null;
    rewards.forEach((raw, i) => {
      if (isObj(raw) && isNum(raw.id)) st.entries.push(entryFromRaw(raw, i));
    });
    st.dirty = false;

    st.sel = keepId && entryOrGhost(keepId) ? keepId : 0;

    if (st.view !== 'days') keepSelectionInView();

    st.lv = Math.min(st.lv, Math.max(0, levelCount(st.sel) - 1));
  }

  // A reward the file does not name yet: one of the game's, or one the mod adds itself (the wind rewards).
  function ghostOf(id) {
    if (!st.byId[id] && !st.builtIn[id]) return null;

    if (!st.ghosts[id]) st.ghosts[id] = entryFromRaw(st.byId[id] ? { id, enabled: true } : clone(st.builtIn[id]), -1);

    return st.ghosts[id];
  }

  // Level n of a reward is its ID + n - 1, so a new reward needs room for ten levels.
  function usedIds() {
    const used = new Set();

    st.game.forEach(t => t.levels.forEach(l => used.add(l.id)));
    Object.keys(st.builtIn).forEach(id => range(isArr(st.builtIn[id].levels) ? st.builtIn[id].levels.length : 1).forEach(i => used.add(Number(id) + i)));
    st.entries.filter(e => !e.removed).forEach(e => range(e.levels.length).forEach(i => used.add(e.id + i)));

    return used;
  }

  function nextFreeId() {
    const used = usedIds();

    for (let id = FIRST_NEW; id + MAX_LEVELS - 1 <= LAST_ID; id++) {
      if (range(MAX_LEVELS).every(i => !used.has(id + i))) return id;
    }

    return 0;
  }

  // A new reward starts from a game reward the game offers, which gives it its look and needs.
  function freshCopyFrom() {
    const t = st.game.find(x => x.available === 1) || st.game[0];

    return t ? t.id : 0;
  }

  const levelPools = (src, i) => src && src.levels.length ? clone(src.levels[Math.min(i, src.levels.length - 1)].pools) : {};

  function copyInto(srcId) {
    const src = st.byId[srcId];
    const id = nextFreeId();

    if (!src || !id) return null;

    const e = entryFromRaw({
      id, enabled: true, copyFrom: srcId, name: { en: src.name.en + ' (copy)', zh: src.name.zh + '（副本）' },
      levels: src.levels.map((l, i) => ({ cost: l.cost, pools: levelPools(src, i) }))
    }, -1);

    st.entries.push(e);
    markChanged(e);

    return e;
  }

  function newReward() {
    const id = nextFreeId();

    if (!id) return null;

    const icon = cat.defaultIcon;
    const copyFrom = freshCopyFrom();
    const src = st.byId[copyFrom];
    const e = entryFromRaw({
      id, enabled: true, copyFrom, name: { en: 'New reward', zh: '新奖励' },
      fields: { WebIcon: '../../' + icon.icon, Icon: icon.unityIcon },
      levels: [5, 10, 15].map((cost, i) => ({ cost, pools: levelPools(src, i) })), gives: []
    }, -1);

    e.autoText = true;
    st.entries.push(e);
    markChanged(e);

    return e;
  }

  // ---- Levels ----

  function addLevel(e) {
    const gameLevel = e.game && e.levels.length < e.game.levels.length ? e.game.levels[e.levels.length] : null;

    if (gameLevel) {
      putBackGameLevel(e, gameLevel);

      return;
    }

    const costs = e.levels.map(l => l.cost);
    const last = e.levels[e.levels.length - 1];

    e.levels.push({
      cost: Math.max(0, Math.round(nextStep(costs))), gameCost: null, name: null, gameName: null,
      desc: last && last.desc ? clone(last.desc) : null, gameDesc: last ? last.gameDesc : null,
      pools: last ? clone(poolsOf(last)) : {}, gamePools: {}, isNew: true, raw: null
    });
    e.gives.forEach(g => numsOf(g.fx).forEach(p => {
      const per = g.per[p.key];

      per.push(Math.max(p.min, round2(nextStep(per))));
    }));

    if (canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  // A game level the player removed comes back as the game has it, and keeps the game's effect while the effects are untouched.
  function putBackGameLevel(e, gameLevel) {
    e.levels.push({
      cost: gameLevel.cost, gameCost: gameLevel.cost, name: null, gameName: gameLevel.name,
      desc: null, gameDesc: gameLevel.desc, pools: null, gamePools: gameLevel.pools, isNew: false, raw: null
    });

    if (e.givesTouched) e.gives.forEach(g => numsOf(g.fx).forEach(p => g.per[p.key].push(g.per[p.key][g.per[p.key].length - 1])));
    else e.gives = baseGives(e, e.base, e.levels.length);

    markChanged(e);
  }

  // Removing a game level switches it off in the game, along with any game level above it.
  function removeLevel(e) {
    if (e.levels.length < 2) return;

    const removed = e.levels.pop();

    e.gives.forEach(g => numsOf(g.fx).forEach(p => { g.per[p.key].pop(); }));

    if (removed.isNew && canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  // ---- Gives ----

  // ---- The day table: which pools each day draws from, and how many from each ----

  const gameDay = day => st.gameDays.find(x => x.day === day) || null;
  const dayPools = day => st.dayEdits[day] || (gameDay(day) ? gameDay(day).pools : []);
  const dayName = day => L('Day ' + day, '第' + day + '天');

  // A day that matches the game's again is dropped from the file.
  function setDayPools(day, pools) {
    const game = gameDay(day);
    const isSame = !!game && game.pools.length === pools.length && game.pools.every((p, i) => p.pool === pools[i].pool && p.amount === pools[i].amount);

    if (isSame) delete st.dayEdits[day];
    else st.dayEdits[day] = pools;

    st.dirty = true;
    st.drawn = null;
  }

  function setDayAmount(day, pool, amount) {
    if (!isFinite(amount)) return;

    setDayPools(day, dayPools(day).map(p => p.pool === pool ? { pool, amount: Math.max(0, Math.round(amount)) } : p));
  }

  function stepDay(key, dir) {
    const pool = Number(key.slice(2));
    const p = dayPools(st.day).find(x => x.pool === pool);

    if (p) setDayAmount(st.day, pool, p.amount + dir);

    render();
  }

  function moveDayPool(day, pool, dir) {
    const pools = dayPools(day).slice();
    const i = pools.findIndex(p => p.pool === pool);
    const j = i + dir;

    if (i < 0 || j < 0 || j >= pools.length) return;

    pools.splice(j, 0, pools.splice(i, 1)[0]);
    setDayPools(day, pools);
  }

  // The reward as the game would have it with this tab's changes, unsaved ones too, or null when it is not in the game.
  function liveReward(id) {
    const own = entryOf(id);

    if (own && own.enabled) return own;

    if (st.byId[id]) return st.byId[id].available === 0 ? null : ghostOf(id);

    return own ? null : ghostOf(id);
  }

  // A level by the name the player sees on its card.
  const levelLabel = (look, i) => levelNameOf(look, i, st.lang);

  // Every pool's reward levels with their weights, in one pass over the rewards.
  function poolIndex() {
    const index = {};

    allIds().forEach(id => {
      const look = liveReward(id);

      if (!look) return;

      look.levels.forEach((l, i) => {
        const pools = poolsOf(l);

        Object.keys(pools).forEach(pool => {
          if (pools[pool] > 0) (index[pool] = index[pool] || []).push({ id: id + i, name: levelLabel(look, i), icon: look.icon, weight: pools[pool] });
        });
      });
    });

    return index;
  }

  const byWeight = (a, b) => b.weight - a.weight || a.id - b.id;

  const poolMembers = (pool, index) => ((index || poolIndex())[pool] || []).slice().sort(byWeight);

  // The days whose draw uses a pool, as runs of days in a row: [[1, 4], [7, 7]].
  function poolDayRuns(pool) {
    const runs = [];

    st.gameDays.forEach((x, k) => {
      if (!dayPools(x.day).some(p => p.pool === pool)) return;

      const run = runs[runs.length - 1];

      if (run && run.k === k - 1) {
        run.to = x.day;
        run.k = k;
        run.days++;
      } else runs.push({ from: x.day, to: x.day, k, days: 1 });
    });

    return runs;
  }

  // "Days 1-4, 7". The game keeps using the last day after it, so a run that reaches it reads "12+". A long list is cut
  // to its first few runs and a count.
  function daysText(runs) {
    const first = st.gameDays.length ? st.gameDays[0].day : 0;
    const last = st.gameDays.length ? st.gameDays[st.gameDays.length - 1].day : -1;
    const runText = r => r.to === last ? r.from + '+' : r.from === r.to ? String(r.from) : r.from + '-' + r.to;

    if (runs.length === 1 && runs[0].from === first && runs[0].to === last) return L('Every day', '每天');

    if (runs.length > 4) {
      const total = runs.reduce((n, r) => n + r.days, 0);
      const from = runs[0].from;
      const to = runs[runs.length - 1].to;

      return L(total + ' days in ' + from + '-' + to, '第' + from + '-' + to + '天中的' + total + '天');
    }

    const isOneDay = runs.length === 1 && runs[0].from === runs[0].to && runs[0].to !== last;

    return L((isOneDay ? 'Day ' : 'Days ') + runs.map(runText).join(', '), '第' + runs.map(runText).join('、') + '天');
  }

  const rewardCount = n => st.lang === 'zh' ? n + '个奖励' : n + (n === 1 ? ' reward' : ' rewards');

  // One end-of-day draw done the way the game does it: each pool in order draws its amount by weight, never the same
  // reward twice, and only the first 3 are shown. It does not know what the player owns or can get, as the game does.
  function testDraw(day) {
    const taken = new Set();
    const picks = [];

    dayPools(day).forEach(p => {
      let left = poolMembers(p.pool).filter(m => !taken.has(m.id));

      for (let n = 0; n < p.amount && left.length; n++) {
        const total = left.reduce((sum, m) => sum + m.weight, 0);
        let roll = Math.floor(Math.random() * total) + 1;
        const pick = left.find(m => (roll -= m.weight) <= 0) || left[left.length - 1];

        taken.add(pick.id);
        picks.push({ pool: p.pool, name: pick.name, icon: pick.icon, isShown: picks.length < 3 });
        left = left.filter(m => m !== pick);
      }
    });

    return picks;
  }

  function daysHtml() {
    const rows = st.gameDays.map(x => {
      const total = dayPools(x.day).reduce((sum, p) => sum + p.amount, 0);

      return '<div class="dy' + (x.day === st.day ? ' is-sel' : '') + '" data-a="day" data-v="' + x.day + '"><span>' + dayName(x.day) +
        (st.dayEdits[x.day] ? ' ' + tag('is-chg', 'CHANGED', '已修改') : '') + '</span><small>' + total + '</small></div>';
    }).join('');
    const empty = st.gameDays.length ? '' : '<div class="nnote">' + L('The game sent no day table.', '游戏没有发来每日抽取表。') + '</div>';

    return '<div class="mr-main">' +
      '<div class="mr-list"><div class="col-h">' + L('DAYS', '天数') + '<small>' + st.gameDays.length + '</small></div>' + empty + rows +
      '<div class="nnote">' + L('After the last day, the game keeps using the last one.', '最后一天之后，游戏一直用最后一天的设置。') + '</div></div>' +
      '<div class="ed">' + (gameDay(st.day) ? dayHtml(st.day) : '') + '</div>' +
      '</div>';
  }

  function dayHtml(day) {
    const pools = dayPools(day);
    const index = poolIndex();
    const free = st.allPools.filter(id => !pools.some(p => p.pool === id));
    const rows = pools.map((p, i) => {
      const count = poolMembers(p.pool, index).length;
      const up = i > 0 ? '<b data-a="dpup" data-v="' + p.pool + '" title="' + L('Draw earlier', '提前抽取') + '">&#9650;</b>' : '<b></b>';
      const down = i < pools.length - 1 ? '<b data-a="dpdn" data-v="' + p.pool + '" title="' + L('Draw later', '推后抽取') + '">&#9660;</b>' : '';

      return '<div class="pl-row dpl"><span class="pl-id" data-a="peek" data-v="' + p.pool + '">' + (st.peek === p.pool ? '&#9660; ' : '&#9654; ') +
        L('Pool', '卡池') + ' ' + p.pool + '<small>' + (count ? rewardCount(count) : L('empty', '空')) + '</small></span>' +
        '<span class="mv">' + up + down + '</span>' +
        stepper('d:' + p.pool, p.amount, L('drawn', '个'), false) +
        '<span class="x" data-a="rmdpool" data-v="' + p.pool + '" title="' + L('Remove', '移除') + '">&#x2715;</span></div>' +
        (st.peek === p.pool ? membersHtml(p.pool) : '');
    }).join('');
    const add = free.length ? poolButton('day', L('ADD A POOL', '添加卡池')) : '';
    const reset = st.dayEdits[day] ? '<span class="gbtn dim" data-a="dayreset">' + L('BACK TO THE GAME\'S', '恢复游戏原设') + '</span>' : '';
    const none = pools.length ? '' : '<div class="nnote">' + L('No pools, so this day offers nothing.', '没有卡池，这一天什么都不会出现。') + '</div>';

    return '<div class="sec-h">' + esc(dayName(day)).toUpperCase() + '</div>' +
      '<div class="nnote">' + L('At the end of this day the game goes down this list in order and draws that many rewards from each pool, never the same one twice. It shows the first 3, so a pool lower down only shows when the ones above come up short. Click a pool to see what is in it.', '这一天结束时，游戏按顺序从每个卡池抽出设定数量的奖励，不会重复。只显示前 3 个，所以下面的卡池只在上面的抽不够时才会出现。点卡池可以看里面有什么。') + '</div>' +
      '<div class="pl">' + rows + none + '<div class="pl-add">' + add + reset + '</div></div>' +
      '<div class="sec-h">' + L('TEST DRAW', '试抽') + '</div>' +
      '<div class="nnote">' + L('Uses the pools and weights in this tab, unsaved changes too. It does not know what you own, your character or your needs, so the game may skip some of these.', '使用本页的卡池和权重，包括没保存的修改。它不知道你拥有什么、你的角色和条件，所以游戏可能会跳过其中一些。') + '</div>' +
      '<div class="pl-add"><span class="gbtn" data-a="draw">' + L('DRAW', '抽取') + '</span></div>' + drawnHtml();
  }

  function membersHtml(pool) {
    const members = poolMembers(pool);
    const total = members.reduce((sum, m) => sum + m.weight, 0);

    if (!members.length) return '<div class="pm"><div>' + L('Nothing is in this pool.', '这个卡池是空的。') + '</div></div>';

    return '<div class="pm">' + members.map(m => '<div>' + esc(m.name) + '<span>' + L('weight', '权重') + ' ' + m.weight + '</span><span>' +
      Math.round(m.weight * 1000 / total) / 10 + '%</span></div>').join('') + '</div>';
  }

  function drawnHtml() {
    if (!st.drawn) return '';

    if (!st.drawn.length) return '<div class="nnote">' + L('Nothing came up: these pools are empty.', '什么都没抽到：这些卡池是空的。') + '</div>';

    return '<div class="pl">' + st.drawn.map(p => '<div class="pl-row drw' + (p.isShown ? '' : ' is-cut') + '"><span class="drw-n"><img src="' + esc(p.icon) + '" alt=""><span>' + esc(p.name) + '</span></span>' +
      '<span class="nm2">' + L('Pool', '卡池') + ' ' + p.pool + '</span><span class="nm2">' + (p.isShown ? '' : L('not shown', '不显示')) + '</span></div>').join('') + '</div>';
  }

  // ===== Saving =====

  const writesGives = e => canEditGives(e) && (e.givesTouched || e.fresh) && e.gives.length > 0;

  function writesLevels(e) {
    const base = e.base;

    if (e.fresh || isAutoOn(e) || writesGives(e) || isArr(e.raw.levels)) return true;

    if (!base || base.levels.length !== e.levels.length) return true;

    return e.levels.some(l => l.desc || l.name || l.pools || l.cost !== l.gameCost);
  }

  function outLevels(e, auto) {
    const writeGivesNow = writesGives(e);

    return e.levels.map((l, i) => {
      const out = l.raw ? clone(l.raw) : {};
      const hadCost = l.raw && 'cost' in l.raw;

      if (l.isNew || hadCost || l.cost !== l.gameCost) out.cost = l.cost;
      else delete out.cost;

      const desc = auto ? { en: autoDesc(e, i, false), zh: autoDesc(e, i, true) } : l.desc ? writeText(l.desc, l.gameDesc) : undefined;
      const name = l.name ? writeText(l.name, l.gameName) : undefined;
      const pools = l.pools ? poolsOut(l.pools) : null;

      if (desc) out.description = desc;
      else delete out.description;

      if (name) out.name = name;
      else delete out.name;

      if (pools && Object.keys(pools).length) out.pools = pools;
      else delete out.pools;

      return out;
    });
  }

  function outEntry(e) {
    if (e.frozen) return Object.assign(clone(e.raw), { enabled: e.enabled });

    const auto = isAutoOn(e);
    const out = { id: e.id, enabled: e.enabled };
    const managed = ['id', 'enabled', 'copyFrom', 'name', 'description', 'cost', 'pools', 'needs', 'furniture', 'levels', 'gives', 'fields', 'fromDay', 'layers'];
    const name = writeText(e.name, baseText(e, 'name'));
    const description = auto ? undefined : writeText(e.description, null);

    if (isNum(e.copyFrom)) out.copyFrom = e.copyFrom;

    if (e.name || e.fresh || e.kind === 'new') out.name = name;

    if (description) out.description = description;

    if (e.needs) out.needs = e.needs.slice();

    if (e.furniture) out.furniture = e.furniture.slice();

    const fields = rulesFields(e);

    if (e.fresh) Object.assign(fields, { Icon: e.unityIcon, WebIcon: '../../' + e.icon });

    if (Object.keys(fields).length) out.fields = fields;

    if (e.rules.fromDay !== e.gameRules.fromDay) out.fromDay = e.rules.fromDay;

    if (e.rules.layers !== e.gameRules.layers) out.layers = e.rules.layers;

    if (writesLevels(e)) out.levels = outLevels(e, auto);

    if (writesGives(e)) out.gives = e.gives.map(g => writeGive(g, e.levels.length));
    else if (isArr(e.raw.gives) && !e.givesTouched) out.gives = clone(e.raw.gives);

    Object.keys(e.raw).forEach(k => {
      if (managed.indexOf(k) < 0) out[k] = clone(e.raw[k]);
    });

    return out;
  }

  const sortedIds = ids => ids.slice().sort((a, b) => a - b).join();

  // The file's own fields, with each rule written only while it differs from the game's.
  function rulesFields(e) {
    const fields = isObj(e.raw.fields) ? clone(e.raw.fields) : {};
    const r = e.rules;
    const g = e.gameRules;
    const put = (key, value, isSame) => {
      if (isSame) delete fields[key];
      else fields[key] = value;
    };

    put('IsReget', r.isReget, r.isReget === g.isReget);
    put('EndlessMode', r.endless, r.endless === g.endless);
    put('PlayerSelectId', r.chars.slice(), sortedIds(r.chars) === sortedIds(g.chars));

    return fields;
  }

  // Changed days in day order; an entry the tab could not read is kept as it was.
  function daysOut() {
    const days = Object.keys(st.dayEdits).map(Number).sort((a, b) => a - b).map(day => ({
      day,
      pools: st.dayEdits[day].map(p => ({ pool: p.pool, amount: p.amount }))
    }));
    const unread = isArr(st.file.days) ? st.file.days.filter(x => !(isObj(x) && isNum(x.day) && isArr(x.pools))).map(clone) : [];

    return days.concat(unread);
  }

  function toFile() {
    const file = clone(st.file);
    const rewards = isArr(st.file.rewards) ? st.file.rewards : [];
    const out = [];

    rewards.forEach((raw, i) => {
      const e = st.entries.find(x => x.rawIndex === i);

      if (!e) out.push(clone(raw));
      else if (e.removed) return;
      else if (e.touched) out.push(outEntry(e));
      else out.push(Object.assign(clone(raw), { enabled: e.enabled }));
    });
    st.entries.filter(e => e.rawIndex < 0 && !e.removed).forEach(e => out.push(outEntry(e)));
    file.rewards = out;

    const days = daysOut();

    if (days.length) file.days = days;
    else delete file.days;

    return file;
  }

  // Why the tab will not save right now, or '' when it can.
  function saveBlocker() {
    if (!st.file) return L('The rewards file could not be read, so this tab will not save over it.', '无法读取奖励文件，本页不会覆盖它。');

    const empty = st.entries.find(e => !e.removed && e.fresh && !e.gives.length);

    if (empty) return L('Choose what it gives first: ', '请先选择效果：') + nameOf(empty, st.lang);

    const unpooled = st.entries.find(e => !e.removed && e.enabled && e.kind === 'new' && e.levels.some(l => !Object.keys(poolsOf(l)).length));

    if (unpooled) return L('Put every level in a pool first, or it is never offered: ', '请先把每一级放进卡池，否则永远不会出现：') + nameOf(unpooled, st.lang);

    return '';
  }

  function save() {
    const blocker = saveBlocker();

    if (blocker) {
      toast(esc(blocker));

      return;
    }

    st.pending = toFile();
    send('save|' + JSON.stringify(st.pending));
  }

  // ===== Drawing the tab =====

  const isOwn = e => e.kind === 'new';

  function render() {
    const root = st.root;

    if (!root) return;

    const list = root.querySelector('.mr-list');
    const ed = root.querySelector('.ed');
    const open = st.view === 'days' ? 'd' + st.day : 'r' + st.sel;
    const edScroll = ed && open === st.edOpen ? ed.scrollTop : 0;

    if (list) st.listScroll = list.scrollTop;

    st.edOpen = open;
    root.innerHTML = topHtml() + bannersHtml() + (st.data ? mainHtml() : '<div class="mr-wait">' + L('Reading the rewards...', '正在读取奖励...') + '</div>');
    fitHeight();

    const newList = root.querySelector('.mr-list');
    const newEd = root.querySelector('.ed');

    if (newList) newList.scrollTop = st.listScroll;

    if (newEd) newEd.scrollTop = edScroll;

    footer();
  }

  function topHtml() {
    const fileCount = st.entries.filter(e => !e.removed).length;
    const allCount = allIds().length;
    const chip = (v, en, zh, n) => '<span data-a="view" data-v="' + v + '"' + (st.view === v ? ' class="is-on"' : '') + '>' + L(en, zh) + ' ' + n + '</span>';

    return '<div class="mr-top">' +
      '<div class="mr-head"><div class="mr-title">' + L('END-OF-DAY REWARDS', '每日奖励') + '</div>' +
      '<div class="mr-sub">' + L('What each reward costs, its levels, what it gives and the pools it comes from. Saving applies at once.', '每个奖励的消耗、等级、效果和所在卡池。保存后立即生效。') + '</div></div>' +
      '<span class="key-btn" data-a="discard">' + L('DISCARD', '放弃') + '</span>' +
      '<span class="key-btn' + (st.dirty ? ' is-due' : '') + '" data-a="save">' + L('SAVE', '保存') + '</span>' +
      '</div>' +
      '<div class="mr-bar">' +
      '<div class="mr-chips">' + chip('file', 'CHANGED', '已修改', fileCount) + chip('all', 'ALL REWARDS', '全部奖励', allCount) + chip('days', 'DAY DRAW', '每日抽取', st.gameDays.length) + '</div>' +
      (st.view === 'days' ? '<span class="sp"></span>' : searchHtml()) +
      '<span class="new-btn" data-a="new">+ ' + L('NEW REWARD', '新建奖励') + '</span>' +
      '</div>';
  }

  function bannersHtml() {
    let html = '';

    if (st.problems.length) html += '<div class="mr-probs">' + st.problems.map(esc).join('<br>') + '</div>';

    if (st.data && !st.file) {
      html += '<div class="mr-bad">' + L('The rewards file could not be read, so this tab will not save over it. Fix or delete MoreRewards.Rewards.json, then restart the game.', '无法读取奖励文件，本页不会覆盖它。请修复或删除 MoreRewards.Rewards.json，然后重启游戏。') + '</div>';
    }

    return html;
  }

  // ---- The list ----

  function allIds() {
    const ids = st.game.map(t => t.id).concat(Object.keys(st.builtIn).map(Number));

    st.entries.forEach(e => {
      if (!e.removed && ids.indexOf(e.id) < 0) ids.push(e.id);
    });

    return ids;
  }

  function mainHtml() {
    if (st.view === 'days') return daysHtml();

    const sel = entryOrGhost(st.sel);

    return '<div class="mr-main">' +
      '<div class="mr-list" data-list="">' + rowsHtml() + '</div>' +
      '<div class="ed">' + (sel ? editorHtml(sel) : '') + '</div>' +
      '</div>';
  }

  // The list's inside, which the search redraws on its own.
  function rowsHtml() {
    const ids = listIds();
    const isAll = st.view === 'all';
    let empty = '';

    if (!ids.length && st.q.trim()) empty = noMatchHtml();
    else if (!ids.length) {
      empty = '<div class="nomatch">' + L('You have not changed any rewards yet.', '还没有改过任何奖励。') +
        '<br><span class="gbtn dim" data-a="view" data-v="all">' + L('SHOW ALL REWARDS', '查看全部奖励') + '</span></div>';
    }

    return '<div class="col-h">' + L(isAll ? 'ALL REWARDS' : 'CHANGED', isAll ? '全部奖励' : '已修改') + '<small>' + ids.length + '</small></div>' + empty + ids.map(rowHtml).join('');
  }

  function rowTag(e, id) {
    if (!e) return st.builtIn[id] ? tag('src-mr', 'MORE REWARDS', '更多奖励') : '';

    if (isOwn(e)) return tag('new', 'NEW', '新');

    return tag('is-chg', 'CHANGED', '已修改');
  }

  function rowHtml(id) {
    const e = entryOf(id);
    const t = st.byId[id];
    const look = e || ghostOf(id);
    const name = look ? nameOf(look, st.lang) : '#' + id;
    const icon = look ? look.icon : '';
    const count = look ? look.levels.length : 0;
    const desc = look && count ? descOf(look, 0, st.lang) : '';
    const cls = ['ab'];

    if (id === st.sel) cls.push('is-sel');

    if (!e && t && t.available === 0) cls.push('is-out');

    if (e && !e.enabled) cls.push('is-off');

    const toggle = e ? '<label class="setting-toggle" title="' + esc(e.kind === 'changed' ? L('Switch your changes on or off', '开关你的修改') : L('Switch this reward on or off', '开关此奖励')) + '">' +
      '<input type="checkbox" data-a="tog" data-v="' + id + '"' + (e.enabled ? ' checked' : '') + '><span class="track"></span><span class="knob"></span></label>' : '<span></span>';

    return '<div class="' + cls.join(' ') + '" data-a="sel" data-v="' + id + '">' +
      '<img src="' + esc(icon) + '" alt="">' +
      '<div><div class="nm">' + esc(name) + '</div>' + (desc ? '<div class="ds">' + esc(desc) + '</div>' : '') + '<div class="mt">' + rowTag(e, id) + '<span class="lvn">' + (st.lang === 'zh' ? count + '级' : count + (count === 1 ? ' level' : ' levels')) + '</span></div></div>' +
      toggle + '</div>';
  }

  // ---- The editor ----

  // The selected level's card. In the game every level is a reward of its own, so the card shows only that level.
  function previewHtml(e) {
    const lang = st.pvLang;
    const Lp = (en, zh) => lang === 'zh' ? zh : en;
    const n = e.levels.length;
    const i = Math.max(0, Math.min(st.lv, n - 1));
    const desc = descOf(e, i, lang);
    const cost = n ? e.levels[i].cost : 0;

    return '<div class="pv">' +
      '<img src="' + esc(e.icon) + '" alt="">' +
      '<div><div class="pv-name">' + esc(levelNameOf(e, i, lang)) + '</div>' +
      (desc ? '<div class="pv-desc">' + esc(desc) + '</div>' : '<div class="pv-desc is-empty">' + Lp('Pick what it gives and its description shows up here.', '选好效果后，描述会显示在这里。') + '</div>') +
      '<div class="pv-cost">' + Lp('Cost', '消耗') + ' <b>' + numText(cost) + '</b> ' + Lp('survival points', '生存点') + '</div></div>' +
      '</div>';
  }

  function whyHtml(e, ghost) {
    const note = text => '<div class="nnote">' + text + '</div>';
    let html;

    if (e.frozen && !ghost) html = note(L('This reward uses parts of the file the tab cannot edit. Change it in MoreRewards.Rewards.json.', '此奖励使用了本页无法编辑的文件内容。请在 MoreRewards.Rewards.json 中修改。'));
    else if (ghost && e.kind === 'builtIn') html = note(L('More Rewards adds this reward while Wind Power is installed. Change it here and your version replaces it.', '安装Wind Power时，更多奖励会加入此奖励。在这里修改后，你的版本会替换它。'));
    else if (ghost) html = note(L('This is one of the game\'s rewards, and it hasn\'t been changed.', '这是游戏中的奖励，没有改动过。'));
    else if (e.kind === 'builtIn') html = note(L('Your version of a reward More Rewards adds. Reset puts back the mod\'s.', '你修改过的更多奖励自带奖励。重置可恢复模组的版本。'));
    else if (e.fresh) html = note(L('A reward of your own. Pick its icon, name, effects and pools. The description is made from the numbers.', '你自己的奖励。选图标、名称、效果和卡池。描述会根据数字生成。'));
    else if (e.kind === 'new') html = note(L('A new reward, copied from', '一个新奖励，复制自') + ' <b>' + esc(e.copySrc ? textIn(e.copySrc.name, st.lang) : '#' + e.copyFrom) + '</b>');
    else html = note(L('You can change what it gives, its cost, levels and text here.', '这里可以改效果、消耗、等级和文字。'));

    if (!ghost && !e.frozen && e.givesLocked) html += note(L('You can change this reward\'s cost, levels and text here, but not what it gives.', '这里可以修改此奖励的消耗、等级和文字，但不能修改它的效果。'));

    if (hasNoScript(e)) html += note(L('The game\'s code runs this reward, so there is no effect to show. Any effect you add works on top of it.', '此奖励由游戏的代码运行，没有可显示的效果。添加的效果会额外生效。'));

    return html;
  }

  function lookHtml(e) {
    const name = e.name || { en: '', zh: '' };

    return '<div class="sec-h">' + L('LOOK', '外观') + '<small>' + L('Icon and name', '图标和名称') + '</small></div>' +
      '<div class="look">' +
      '<div class="look-ic" data-a="icon"><img src="' + esc(e.icon) + '" alt=""><span>' + L('CHANGE', '更换') + '</span></div>' +
      '<div class="look-f">' +
      '<span></span><span class="k">English</span><span class="k">中文</span>' +
      '<span class="k">' + L('Name', '名称') + '</span>' +
      '<input data-a="txt" data-k="name:en" value="' + esc(name.en) + '">' +
      '<input data-a="txt" data-k="name:zh" value="' + esc(name.zh) + '">' +
      '</div></div>';
  }

  function srcTag(f) {
    return f.src === 'mr' ? tag('src-mr', 'MORE REWARDS', '更多奖励') : tag('src-game', 'GAME', '游戏');
  }

  function givesHtml(e) {
    const editable = canEditGives(e);
    const canRemove = editable && (e.fresh || e.gives.length > 1);
    const head = '<div class="sec-h">' + L('WHAT IT GIVES', '效果') + '<small>' + L('Effects from the game and More Rewards', '来自游戏和更多奖励的效果') + '</small></div>';

    if (!e.gives.length) {
      if (!editable) return '';

      return head + '<div class="gv"><div class="gv-empty">' + L('Nothing yet. A reward needs at least one effect.', '还没有效果。奖励至少需要一个效果。') +
        '<br><span class="gbtn" data-a="add">' + L('CHOOSE WHAT IT GIVES', '选择效果') + '</span></div></div>';
    }

    const rows = e.gives.map((g, gi) => '<div class="gv-row">' +
      '<img src="' + esc(fxIcon(g.fx)) + '" alt="">' +
      '<div><div class="gv-n">' + esc(fxName(g.fx, st.lang === 'zh')) + srcTag(g.fx) + '</div><div class="gv-w">' + esc(L(g.fx.what[0], g.fx.what[1])) + '</div></div>' +
      '<div class="gv-picks">' + picksOf(g.fx).map(p => pickHtml(e, g, gi, p)).join('') + '</div>' +
      (editable ? '<a data-a="chg" data-c="' + gi + '">' + L('CHANGE', '更换') + '</a>' : '<span></span>') +
      (canRemove ? '<span class="x" data-a="rm" data-c="' + gi + '" title="' + L('Remove', '移除') + '">&#x2715;</span>' : '<span></span>') +
      '</div>').join('');
    const add = editable && e.gives.length < MAX_GIVES ? '<div class="gv-add" data-a="add">+ ' + L('ADD ANOTHER EFFECT', '再添加一个效果') + '</div>' : '';

    return head + '<div class="gv">' + rows + add + '</div>';
  }

  function levelsHtml(e) {
    const n = e.levels.length;
    const valuesLocked = e.givesLocked;
    const total = e.levels.reduce((sum, l) => sum + l.cost, 0);
    const chips = e.levels.map((l, i) => '<span data-a="lv" data-v="' + i + '"' + (i === st.lv ? ' class="is-on"' : '') + '>Lv.' + (i + 1) + '</span>').join('');
    const canAdd = n < MAX_LEVELS;
    const cols = [];

    e.gives.forEach((g, gi) => numsOf(g.fx).forEach(p => cols.push({ g, gi, p })));

    const many = e.gives.length > 1;
    const head = '<tr><th>' + L('LEVEL', '等级') + '</th><th>' + L('COST', '消耗') + '</th>' +
      cols.map(c => '<th>' + esc(L(c.p.en, c.p.zh).toUpperCase()) + (many ? '<small>' + esc(fxName(c.g.fx, st.lang === 'zh')) + '</small>' : '') + '</th>').join('') + '<th></th></tr>';
    const rows = e.levels.map((l, i) => '<tr' + (i === st.lv ? ' class="is-on"' : '') + '>' +
      '<td class="l" data-a="lv" data-v="' + i + '">Lv.' + (i + 1) + '</td>' +
      '<td>' + stepper('c:' + i, l.cost, '', false) + '</td>' +
      cols.map(c => '<td>' + stepper('g:' + c.gi + ':' + c.p.key + ':' + i, c.g.per[c.p.key][i], unitOf(c.p), valuesLocked) + '</td>').join('') +
      (i > 0 && i === n - 1 ? '<td class="x" data-a="rmlv" title="' + L('Remove', '移除') + '">&#x2715;</td>' : '<td></td>') +
      '</tr>').join('');

    return '<div class="lvs">' + chips +
      (canAdd ? '<span class="is-add" data-a="addlv">+ ' + L('LEVEL', '等级') + '</span>' : '') +
      '<span class="total">' + L('To max level', '升满共需') + ' <b>' + numText(total) + '</b> ' + L('points', '点') + '</span></div>' +
      '<table class="lt">' + head + rows + '</table>' + chainHtml(e) + switchedOffHtml(e) + poolsHtml(e) + offeredHtml(e);
  }

  function chainHtml(e) {
    if (e.levels.length < 2) return '';

    return '<div class="nnote">' + L('In the game each level is a reward of its own, with its own name, text and cost. A level only comes up once the player owns the one below it.', '游戏里每一级都是单独的奖励，有自己的名称、描述和消耗。玩家拥有前一级后，才会出现下一级。') + '</div>';
  }

  function switchedOffHtml(e) {
    const n = e.levels.length;

    if (!e.game || n >= e.game.levels.length) return '';

    return '<div class="nnote">' + L('The game\'s levels above Lv.' + n + ' are switched off, so they are never offered. + LEVEL puts them back.', '游戏在 Lv.' + n + ' 以上的等级已关闭，不会再出现。点「+ 等级」可以加回来。') + '</div>';
  }

  // The pools the selected level is drawn from. A game level keeps the game's pools; you can add more and set weights.
  function poolsHtml(e) {
    const i = Math.min(st.lv, e.levels.length - 1);
    const l = e.levels[i];
    const pools = poolsOf(l);
    const ids = Object.keys(pools).map(Number).sort((a, b) => a - b);
    const free = st.allPools.filter(id => !(id in pools));
    const index = poolIndex();
    const about = id => {
      const runs = poolDayRuns(id);

      return '<small>' + rewardCount(poolMembers(id, index).length) + L(', ', '，') + (runs.length ? daysText(runs) : L('not in the day draw', '不在每日抽取中')) + '</small>';
    };
    const rows = ids.map(id => '<div class="pl-row"><span class="pl-id">' + L('Pool', '卡池') + ' ' + id + (id in l.gamePools ? tag('src-game', 'GAME', '游戏') : '') + about(id) + '</span>' +
      stepper('p:' + id + ':' + i, pools[id], L('weight', '权重'), false) +
      (id in l.gamePools ? '<span></span>' : '<span class="x" data-a="rmpool" data-v="' + id + '" title="' + L('Remove', '移除') + '">&#x2715;</span>') + '</div>').join('');
    const add = free.length ? poolButton('reward', L('ADD TO A POOL', '加入卡池')) : '';
    const same = e.levels.length > 1 ? '<span class="gbtn dim" data-a="poolsall">' + L('USE THESE POOLS FOR EVERY LEVEL', '所有等级使用这些卡池') + '</span>' : '';
    const none = ids.length ? '' : '<div class="nnote">' + L('Not in any pool, so the game never offers this level.', '不在任何卡池中，游戏永远不会提供这一级。') + '</div>';

    return '<div class="sec-h">' + L('POOLS', '卡池') + ' (Lv.' + (i + 1) + ')<small>' + L('Where the game draws it from. A higher weight comes up more often.', '游戏从哪些卡池抽取它。权重越高越常出现。') + '</small></div>' +
      '<div class="pl">' + rows + none + '<div class="pl-add">' + add + same + '</div></div>' +
      (Object.keys(l.gamePools).length ? '<div class="nnote">' + L('You can\'t remove the game\'s pools, only change their weight.', '游戏原有的卡池不能移除，只能改权重。') + '</div>' : '');
  }

  // The rules on level 1: when it can come up, for which runs and characters, what it needs first, and buying it again.
  function offeredHtml(e) {
    const base = e.base;
    const r = e.rules;
    const row = (key, en, zh, own, game) => '<span class="k">' + L(en, zh) + '</span>' +
      '<input data-a="ids" data-k="' + key + '" value="' + esc(own ? own.join(', ') : '') + '" placeholder="' + esc(game.length ? game.join(', ') : L('none', '无')) + '">' +
      '<span class="nm2">' + esc((own || game).map(id => idName(id)).join(', ')) + '</span>';
    const fromDay = r.fromDay > 0 ? L('Offered from day ' + r.fromDay, '第' + r.fromDay + '天起出现') : L('Offered from the first day', '从第一天起出现');
    const also = base && base.also.length ? '<span class="k">' + L('Also needs', '还需要') + '</span><span class="ro">' + esc(base.also.join('; ')) + '</span>' +
      '<span class="nm2">' + L('Set by the game', '游戏设定') + '</span>' : '';

    return '<div class="sec-h">' + L('WHEN IT IS OFFERED', '出现条件') + '<small>' + L('When the game can offer it, and to whom.', '游戏什么时候、给谁提供它。') + '</small></div>' +
      '<div class="tx">' +
      '<span class="k">' + L('From day', '从第几天') + '</span>' + stepper('r:fromDay', r.fromDay, '', false) + '<span class="nm2">' + fromDay + '</span>' +
      '<span class="k">' + L('Runs', '模式') + '</span>' + runsHtml(r.endless, e.gameRules.endless) + '<span></span>' +
      '<span class="k">' + L('Characters', '角色') + '</span><div class="chw">' + charsHtml(r.chars) +
      (r.chars.length ? '' : '<span class="nm2">' + L('None picked, so every character gets it', '未选，所有角色都能拿到') + '</span>') + '</div>' +
      row('needs', 'Rewards you need first', '需要先拥有的奖励', e.needs, base ? base.needs : []) +
      row('furniture', 'Furniture you can build', '可以建造的家具', e.furniture, base ? base.furniture : []) +
      '<span></span><span class="tx-n">' + L('Type IDs split by commas. Leave a box empty to keep the game\'s list.', '填ID，用逗号分隔。留空则用游戏原有的。') + '</span>' +
      also +
      '</div>' + buyAgainHtml(e);
  }

  // EndlessMode 6 is endless runs only. Any other value the game uses is kept and shown as it is.
  function runsHtml(value, game) {
    const options = [[0, 'Any run', '任何模式'], [6, 'Endless Mode only', '仅无尽模式']];

    [game, value].forEach(v => {
      if (!options.some(o => o[0] === v)) options.push([v, 'As the game has it (' + v + ')', '游戏原设（' + v + '）']);
    });

    return '<select class="nsel" data-a="rule" data-k="endless">' +
      options.map(o => '<option value="' + o[0] + '"' + (o[0] === value ? ' selected' : '') + '>' + L(o[1], o[2]) + '</option>').join('') + '</select>';
  }

  function charsHtml(chosen) {
    if (!st.characters.length) return '<span class="nm2">' + L('The game sent no characters.', '游戏没有发来角色。') + '</span>';

    return '<div class="lvs chs">' + st.characters.map(c => '<span data-a="char" data-v="' + c.id + '"' + (chosen.indexOf(c.id) >= 0 ? ' class="is-on"' : '') + '>' + esc(textIn(c.name, st.lang)) + '</span>').join('') + '</div>';
  }

  // Bought again, the reward's effect stacks once more each time, up to its limit.
  function buyAgainHtml(e) {
    const r = e.rules;
    const hasEffect = e.gives.length > 0 || e.gameRules.layers > 0;
    const stacks = r.isReget && hasEffect ? '<div class="tx"><span class="k">' + L('Stacks up to', '最多叠加') + '</span>' + stepper('r:layers', r.layers, L('times', '次'), false) +
      '<span class="nm2">' + L('Each time it is bought, its effect stacks once more, up to this many.', '每买一次，效果多叠加一层，最多到这个数。') + '</span></div>' : '';

    return '<label class="chk"><input type="checkbox" data-a="reget"' + (r.isReget ? ' checked' : '') + '>' + L('Can be offered and bought again after you own it', '拥有后还会出现，可以再次购买') + '</label>' + stacks;
  }

  function toggleChar(e, id) {
    const chars = e.rules.chars;

    e.rules.chars = chars.indexOf(id) >= 0 ? chars.filter(c => c !== id) : chars.concat([id]).sort((a, b) => a - b);
    markChanged(e);
  }

  // From day is 0 or more (0 is the first day); a reward stacks at least once.
  function setRule(e, key, value) {
    e.rules[key] = Math.max(key === 'layers' ? 1 : 0, Math.round(value));

    // A copy needs its own effect rows to stack differently from the reward it copies.
    if (key === 'layers' && !e.game && canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  // A reward's or a level's name, for the IDs in "when it is offered".
  function idName(id) {
    for (const t of st.game) {
      const k = t.levels.findIndex(l => l.id === id);

      if (k >= 0) return textIn(t.levels[k].name, st.lang) || '#' + id;
    }

    const e = entryOrGhost(id);

    return e ? nameOf(e, st.lang) : '#' + id;
  }

  function textHtml(e) {
    const open = st.textOpen;
    const head = '<div class="fold"><div class="fold-t" data-a="fold">' + (open ? '&#9660; ' : '&#9654; ') + L('TEXT THE PLAYER SEES', '玩家看到的文字') + '</div>';

    if (!open) return head + '</div>';

    const canAuto = e.gives.length > 0 && e.gives.every(g => !g.fx.raw) && !e.givesLocked;
    const auto = isAutoOn(e);
    const i = Math.min(st.lv, e.levels.length - 1);
    const d = descTarget(e);
    const inputs = (key, own, game) => ['en', 'zh'].map(lang => '<input data-a="txt" data-k="' + key + ':' + lang + '" value="' + esc(own ? own[lang] : '') + '" placeholder="' + esc(textIn(game, lang)) + '">').join('');
    const autoCells = (en, zh) => '<span class="auto">' + esc(en) + '</span><span class="auto">' + esc(zh) + '</span>';
    const descLabel = L('Description', '描述') + (auto || d.perLevel ? ' (Lv.' + (i + 1) + ')' : '');
    let html = head;

    if (canAuto) html += '<label class="chk"><input type="checkbox" data-a="auto"' + (auto ? ' checked' : '') + '>' + L('Write the description from the numbers above', '根据上面的数字自动生成描述') + '</label>';

    html += '<div class="tx"><span></span><span class="hd2">ENGLISH</span><span class="hd2">中文</span>';
    html += '<span class="k">' + descLabel + '</span>' + (auto ? autoCells(autoDesc(e, i, false), autoDesc(e, i, true)) : inputs('desc', d.own, d.game));
    if (!e.fresh) html += '<span class="k">' + L('Name', '名称') + '</span>' + inputs('name', e.name, baseText(e, 'name'));

    if (e.levels.length > 1) {
      const l = e.levels[i];
      const fallback = { en: defaultLevelName(e, i, 'en'), zh: defaultLevelName(e, i, 'zh') };

      html += '<span class="k">' + L('Level name', '等级名称') + ' (Lv.' + (i + 1) + ')</span>' + inputs('lname', l.name, fallback);
    }

    html += '</div>';
    html += '<div class="nnote">' + (auto ? L('Untick the box to write your own text.', '取消勾选即可自己写文字。') : L('Empty boxes use the game\'s text, shown in grey.', '留空则使用游戏原文（灰色显示）。')) + '</div>';

    return html + '</div>';
  }

  function actionsHtml(e) {
    const label = isOwn(e) ? L('DELETE THIS REWARD', '删除此奖励') : L('RESET THIS REWARD', '重置此奖励');

    return '<div class="ed-acts"><span class="gbtn dim" data-a="reset">' + label + '</span></div>';
  }

  function footer() {
    if (!st.ctx || typeof st.ctx.footer !== 'function') return;

    try {
      if (st.dirty) st.ctx.footer('<em>' + L('Unsaved changes', '有未保存的修改') + '</em> &middot; ' + L('they apply when you save', '保存后立即生效'), true);
      else st.ctx.footer('<b>' + FILE_NAME + '</b> &middot; ' + L('saved', '已保存'), false);
    } catch (err) {}
  }

  // ===== The effect picker =====

  function openPicker(mode, gi) {
    const e = entryOf(st.sel);
    const win = winEl();

    if (!e || !win) return;

    closeOverlays();

    const current = mode === 'replace' && e.gives[gi] ? e.gives[gi].fx.id : '';

    st.picker = { mode, gi, q: '', src: 'all', cat: 'all', selId: current };
    st.pickerEl = h('<div class="mr-fxp" data-own=""></div>');
    st.pickerEl.addEventListener('click', onClick);
    st.pickerEl.addEventListener('dblclick', onPickerDouble);
    st.pickerEl.addEventListener('input', onInput);
    st.pickerEl.addEventListener('keydown', onPickerKey);
    win.appendChild(st.pickerEl);
    renderPicker(true);
  }

  function pickerHead(e) {
    const p = st.picker;
    const old = p.mode === 'replace' && e.gives[p.gi] ? e.gives[p.gi].fx : null;
    const sub = (old ? L('Replacing', '替换') + ' ' + esc(fxName(old, st.lang === 'zh')) + ' &middot; ' : '') + esc(nameOf(e, st.lang));
    const words = p.q.toLowerCase().split(/\s+/).filter(Boolean);
    const byQuery = cat.list.filter(f => matchesQuery(f, words) && (p.cat === 'all' || f.cat === p.cat));
    const src = (v, en, zh) => '<span data-a="src" data-v="' + v + '"' + (p.src === v ? ' class="is-on"' : '') + '>' + L(en, zh) + '<b>' + byQuery.filter(f => v === 'all' || f.src === v).length + '</b></span>';

    return '<div class="fxp-hd">' +
      '<div class="t">' + L('WHAT IT GIVES', '选择效果') + '<small>' + sub + '</small></div>' +
      '<label class="fxp-q"><span>&#x2315;</span><input data-a="q" value="' + esc(p.q) + '" placeholder="' + esc(L('Search by name, what it does, or a reward that uses it...', '按名称、效果或使用它的奖励搜索...')) + '"></label>' +
      '<div class="fxp-src">' + src('all', 'ALL', '全部') + src('game', 'GAME', '游戏') + src('mr', 'MORE REWARDS', '更多奖励') + '</div>' +
      '<span class="fxp-x" data-a="close" title="' + L('Close', '关闭') + '">&#x2715;</span>' +
      '</div>';
  }

  function detailHtml(e, shown) {
    const f = shown.find(x => x.id === st.picker.selId);

    if (!f) return '<div class="fxd"></div>';

    const count = e.levels.length;
    const shownParams = f.params.filter(p => p.type !== 'hide');
    const users = f.finishes.concat(f.usedBy);
    const notes = [];

    if (f.modes === 'endless') notes.push(L('The game only applies this in Endless mode.', '游戏只在无尽模式中生效。'));

    if (f.mr) notes.push(L('More Rewards runs this effect itself.', '由更多奖励自身实现。'));

    if (f.raw) notes.push(L('A game script with no friendly names yet. Its numbers are the raw fields from the game\'s tables.', '尚未命名的游戏脚本。数值为游戏表中的原始字段。'));

    if (!numsOf(f).length) notes.push(L('This effect has no numbers, so a reward with only this effect is the same at every level.', '此效果没有数值，所以只有此效果的奖励每级都一样。'));

    let html = '<div class="fxd"><div class="fxd-top"><img src="' + esc(fxIcon(f)) + '" alt=""><div>' +
      '<div class="fxd-n">' + esc(fxName(f, st.lang === 'zh')) + '</div>' +
      '<div class="fxd-s">' + srcTag(f) + (f.mr ? [MR_PREFIX + f.mr] : f.scripts).map(s => '<code>' + esc(s) + '</code>').join('') + '</div></div></div>' +
      '<div class="fxd-w">' + esc(L(f.what[0], f.what[1])) + '</div>';

    if (shownParams.length) html += '<h4>' + L('NUMBERS IT TAKES', '数值') + '</h4><table>' + shownParams.map(p => paramRow(p, count)).join('') + '</table>';

    html += '<h4>' + (st.lang === 'zh' ? '显示效果：' + count + '级' : 'HOW IT READS AT ' + count + (count === 1 ? ' LEVEL' : ' LEVELS')) + '</h4>' + readsHtml(e, f, count);

    if (users.length) html += '<h4>' + L('REWARDS THAT USE IT', '使用它的奖励') + '</h4><div class="ub">' + users.map(id => '<span><img src="' + esc(st.byId[id] ? st.byId[id].icon : '') + '" alt="">' + esc(talentName(id)) + '</span>').join('') + '</div>';

    if (notes.length) html += '<h4>' + L('NOTES', '提示') + '</h4>' + notes.map(n => '<div class="fn">' + n + '</div>').join('');

    return html + chooseButton(e, f) + '</div>';
  }

  // ===== The pool picker =====

  // kind is 'day' for the day draw and 'reward' for the selected reward level's pools.
  const poolButton = (kind, label) => '<span class="pk-btn' + (st.menu && st.menu.kind === kind ? ' is-open' : '') + '" data-a="poolmenu" data-v="' + kind + '">+ ' + label + '</span>';

  const selectedLevel = e => e.levels[Math.min(st.lv, e.levels.length - 1)];

  function openPoolMenu(anchor, kind) {
    const win = winEl();

    if (!win) return;

    closeOverlays();
    st.menu = { kind, q: '', at: 0, index: poolIndex(), runs: {} };
    st.menuEl = h('<div class="mr-pools" data-own="">' +
      '<div class="pq"><input data-a="pmq" placeholder="' + esc(L('Search by pool number or a reward in it', '按卡池编号或其中的奖励搜索')) + '" spellcheck="false" autocomplete="off"></div>' +
      '<div class="po-list">' + menuListHtml() + '</div>' +
      '<div class="po-ft">' + L('Up and Down to move, Enter to add, Esc to close', '上下键移动，回车添加，Esc 关闭') + '</div></div>');
    st.menuEl.addEventListener('click', onClick);
    st.menuEl.addEventListener('input', onMenuInput);
    st.menuEl.addEventListener('keydown', onMenuKey);
    win.appendChild(st.menuEl);
    placeMenu(st.menuEl, anchor, win);
    anchor.classList.add('is-open');
    document.addEventListener('mousedown', onMenuDown, true);
    document.addEventListener('scroll', onMenuScroll, true);
    st.menuEl.querySelector('input').focus();
  }

  // Under the button when a few options fit there, else on whichever side has more room, with the list cut to fit. Above
  // the button the menu hangs from its bottom edge, so a search that shortens the list keeps it next to the button.
  function placeMenu(el, anchor, win) {
    const w = win.getBoundingClientRect();
    const a = anchor.getBoundingClientRect();
    const scale = win.offsetWidth ? w.width / win.offsetWidth : 1;
    const top = (a.top - w.top) / scale;
    const bottom = (a.bottom - w.top) / scale;
    const roomBelow = win.offsetHeight - bottom - 14;
    const roomAbove = top - 14;
    const isBelow = roomBelow >= Math.min(el.offsetHeight, 300) || roomBelow >= roomAbove;
    const room = isBelow ? roomBelow : roomAbove;
    const list = el.querySelector('.po-list');

    if (el.offsetHeight > room) list.style.maxHeight = Math.max(120, list.offsetHeight - (el.offsetHeight - room)) + 'px';

    el.style.left = Math.max(10, Math.min((a.left - w.left) / scale, win.offsetWidth - el.offsetWidth - 10)) + 'px';

    if (isBelow) el.style.top = bottom + 4 + 'px';
    else el.style.bottom = win.offsetHeight - top + 4 + 'px';
  }

  function closeMenus() {
    if (!st.menuEl) return false;

    if (st.menuEl.parentNode) st.menuEl.parentNode.removeChild(st.menuEl);

    st.menuEl = null;
    st.menu = null;
    document.removeEventListener('mousedown', onMenuDown, true);
    document.removeEventListener('scroll', onMenuScroll, true);

    const open = st.root ? st.root.querySelector('.pk-btn.is-open') : null;

    if (open) open.classList.remove('is-open');

    return true;
  }

  // The button toggles the picker itself, so a press on it is left to the click.
  function onMenuDown(ev) {
    const t = ev.target;

    if (!st.menuEl || st.menuEl.contains(t) || (t.closest && t.closest('[data-a="poolmenu"]'))) return;

    closeMenus();
  }

  // The menu sits where the button was, so scrolling the page under it closes it.
  function onMenuScroll(ev) {
    if (st.menuEl && !st.menuEl.contains(ev.target)) closeMenus();
  }

  const menuRuns = pool => st.menu.runs[pool] || (st.menu.runs[pool] = poolDayRuns(pool));

  // The pools that can still be added, split into the ones worth picking and the rest.
  function menuGroups() {
    const m = st.menu;
    const e = entryOf(st.sel);
    const taken = m.kind === 'day' ? dayPools(st.day).map(p => p.pool) : e ? Object.keys(poolsOf(selectedLevel(e))).map(Number) : [];
    const words = m.q.toLowerCase().split(/\s+/).filter(w => w);
    const text = pool => ('pool ' + L('', '卡池 ') + pool + ' ' + (m.index[pool] || []).map(x => x.name).join(' ')).toLowerCase();
    const pools = st.allPools.filter(pool => taken.indexOf(pool) < 0 && words.every(w => text(pool).indexOf(w) >= 0));
    const isUseful = m.kind === 'day' ? pool => !!m.index[pool] : pool => menuRuns(pool).length > 0;

    return { useful: pools.filter(isUseful), rest: pools.filter(pool => !isUseful(pool)) };
  }

  function menuOrder() {
    const g = menuGroups();

    return g.useful.concat(g.rest);
  }

  function menuListHtml() {
    const m = st.menu;
    const g = menuGroups();
    const count = g.useful.length + g.rest.length;

    m.at = Math.max(0, Math.min(m.at, count - 1));

    if (!count) return '<div class="po-none">' + L('No pool matches.', '没有匹配的卡池。') + '</div>';

    const isDay = m.kind === 'day';
    const head = (en, zh, noteEn, noteZh) => '<div class="po-sec">' + L(en, zh) + (noteEn ? '<small>' + L(noteEn, noteZh) + '</small>' : '') + '</div>';
    const useful = g.useful.map((pool, k) => poolOptionHtml(pool, k === m.at)).join('');
    const rest = g.rest.map((pool, k) => poolOptionHtml(pool, g.useful.length + k === m.at)).join('');
    const usefulHead = isDay ? head('POOLS WITH REWARDS', '有奖励的卡池') : head('IN THE DAY DRAW', '在每日抽取中');
    const restHead = isDay ? head('EMPTY POOLS', '空卡池', 'These have no rewards in them yet.', '这些卡池里还没有奖励。') :
      head('NOT IN THE DAY DRAW', '不在每日抽取中', 'A reward that is only in these never shows up.', '只在这些卡池里的奖励永远不会出现。');

    return (useful ? usefulHead + useful : '') + (rest ? restHead + rest : '');
  }

  function poolOptionHtml(pool, isAt) {
    const members = poolMembers(pool, st.menu.index);
    const runs = menuRuns(pool);
    const shown = members.slice(0, 3);
    const more = members.length > shown.length ? L(' and ' + (members.length - shown.length) + ' more', ' 等' + members.length + '个') : '';
    const peek = members.length ? '<div class="po-r">' + shown.map(x => '<img src="' + esc(x.icon) + '" alt="">').join('') + '<span>' + esc(shown.map(x => x.name).join(', ') + more) + '</span></div>' : '';

    return '<div class="po' + (isAt ? ' is-at' : '') + (members.length ? '' : ' is-empty') + '" data-a="pmpick" data-v="' + pool + '">' +
      '<div class="po-h"><b>' + L('Pool', '卡池') + ' ' + pool + '</b><span>' + (members.length ? rewardCount(members.length) : L('Empty', '空')) + '</span>' +
      (runs.length ? '<span>' + daysText(runs) + '</span>' : '') + '</div>' + peek + '</div>';
  }

  function renderMenuList() {
    const list = st.menuEl ? st.menuEl.querySelector('.po-list') : null;

    if (!list) return;

    list.innerHTML = menuListHtml();

    const at = list.querySelector('.is-at');

    if (!at) return;

    const head = at.previousElementSibling && at.previousElementSibling.classList.contains('po-sec') ? at.previousElementSibling.offsetHeight : 0;

    if (at.offsetTop - head < list.scrollTop) list.scrollTop = at.offsetTop - head;
    else if (at.offsetTop + at.offsetHeight > list.scrollTop + list.clientHeight) list.scrollTop = at.offsetTop + at.offsetHeight - list.clientHeight;
  }

  function onMenuInput(ev) {
    if (ev.target.getAttribute('data-a') !== 'pmq' || !st.menu) return;

    st.menu.q = ev.target.value;
    st.menu.at = 0;
    renderMenuList();
    st.menuEl.querySelector('.po-list').scrollTop = 0;
  }

  function onMenuKey(ev) {
    if (!st.menu) return;

    // The key stops here. The Mod Manager's Esc would only take the cursor out of the search and leave the menu open.
    if (ev.key === 'Escape') {
      ev.preventDefault();
      ev.stopPropagation();
      closeMenus();

      return;
    }

    if (ev.key === 'ArrowDown' || ev.key === 'ArrowUp') {
      ev.preventDefault();
      st.menu.at += ev.key === 'ArrowDown' ? 1 : -1;
      renderMenuList();
    } else if (ev.key === 'Enter') {
      ev.preventDefault();

      const pool = menuOrder()[st.menu.at];

      if (pool) addPool(pool);
    }
  }

  function addPool(pool) {
    const kind = st.menu ? st.menu.kind : '';
    const e = entryOf(st.sel);

    closeMenus();

    if (kind === 'day') setDayPools(st.day, dayPools(st.day).concat([{ pool, amount: 1 }]));
    else if (kind === 'reward' && e) {
      setPool(selectedLevel(e), pool, 1);
      markChanged(e);
    }

    render();
  }

  // ===== The icon picker =====

  function openIcons(anchor) {
    const win = winEl();

    if (!win || !cat.icons.length) return;

    closeIcons();
    st.iconsEl = h('<div class="mr-icons" data-own="">' + iconsHtml() + '</div>');
    st.iconsEl.addEventListener('click', onClick);
    win.appendChild(st.iconsEl);

    const w = win.getBoundingClientRect();
    const a = anchor.getBoundingClientRect();
    const scale = win.offsetWidth ? w.width / win.offsetWidth : 1;
    const top = Math.max(10, Math.min((a.top - w.top) / scale, win.offsetHeight - st.iconsEl.offsetHeight - 10));
    const left = Math.max(10, Math.min((a.right - w.left) / scale + 10, win.offsetWidth - st.iconsEl.offsetWidth - 10));

    st.iconsEl.style.left = left + 'px';
    st.iconsEl.style.top = top + 'px';
    document.addEventListener('mousedown', onDocDown, true);
  }

  // ===== Toast =====

  function toast(html) {
    const win = winEl();

    closeToast();

    if (!html || !win) return;

    st.toastEl = h('<div class="mr-toast" data-own="">' + html + '</div>');
    win.appendChild(st.toastEl);
    st.toastTimer = setTimeout(closeToast, TOAST_MS);
  }

  // ===== Edits from the editor =====

  function editText(e, key, value) {
    const parts = key.split(':');
    const field = parts[0];
    const lang = parts[1] === 'zh' ? 'zh' : 'en';

    if (field === 'name') e.name = setText(e.name, lang, value);
    else if (field === 'lname') {
      const l = e.levels[Math.min(st.lv, e.levels.length - 1)];

      l.name = setText(l.name, lang, value);
    } else if (field === 'desc' && hasLevelDescs(e)) {
      const l = e.levels[Math.min(st.lv, e.levels.length - 1)];

      l.desc = setText(l.desc, lang, value);
    } else if (field === 'desc') e.description = setText(e.description, lang, value);

    markChanged(e);
  }

  // Unticking keeps what the numbers wrote, so the player starts from that text instead of the game's.
  function setAuto(e, isOn) {
    if (!isOn && isAutoOn(e)) {
      e.levels.forEach((l, i) => { l.desc = { en: autoDesc(e, i, false), zh: autoDesc(e, i, true) }; });
    }

    e.autoText = isOn;
    e.autoManual = !isOn;
    markChanged(e);
  }

  // A stepper key is "c:<level>" for a cost, "p:<pool>:<level>" for a pool weight, or "g:<give>:<param>:<level>" for
  // an effect's number.
  function stepTarget(e, key) {
    const parts = key.split(':');

    if (parts[0] === 'r') return { rule: parts[1], step: 1 };

    if (parts[0] === 'c') return { isCost: true, i: Number(parts[1]), step: 1 };

    if (parts[0] === 'p') return { pool: Number(parts[1]), i: Number(parts[2]), step: 1 };

    const g = e.gives[Number(parts[1])];
    const p = g ? numsOf(g.fx).find(x => x.key === parts[2]) : null;

    return p ? { isCost: false, g, p, i: Number(parts[3]), step: p.step } : null;
  }

  function readValue(e, t) {
    if (t.rule) return e.rules[t.rule];
    if (t.pool) return poolsOf(e.levels[t.i])[t.pool] || 1;

    return t.isCost ? e.levels[t.i].cost : t.g.per[t.p.key][t.i];
  }

  function writeValue(e, t, value) {
    if (t.rule) {
      setRule(e, t.rule, value);

      return;
    }
    if (t.pool) {
      setPool(e.levels[t.i], t.pool, value);
      markChanged(e);

      return;
    }

    if (t.isCost) {
      e.levels[t.i].cost = Math.max(0, Math.round(value));
      markChanged(e);

      return;
    }

    if (e.givesLocked) return;

    t.g.per[t.p.key][t.i] = Math.max(t.p.min, Math.round(value * 10000) / 10000);

    if (canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  function stepValue(e, key, dir) {
    const t = e ? stepTarget(e, key) : null;

    if (!t || !(t.rule || (t.isCost || t.pool ? e.levels[t.i] : t.g.per[t.p.key].length > t.i))) return;

    writeValue(e, t, readValue(e, t) + dir * t.step);
    render();
  }

  // Weights are whole numbers of 1 or more, as the game's are.
  function setPool(l, pool, weight) {
    l.pools = Object.assign({}, l.pools || {});
    l.pools[pool] = Math.max(1, Math.round(weight));
  }

  function removePool(l, pool) {
    if (!l.pools || pool in l.gamePools) return;

    delete l.pools[pool];
  }

  // Every level takes the selected level's pools; a level keeps the pools the game gave it.
  function poolsForAll(e) {
    const from = poolsOf(e.levels[Math.min(st.lv, e.levels.length - 1)]);

    e.levels.forEach(l => {
      const own = {};

      Object.keys(from).forEach(k => {
        if (l.gamePools[k] !== from[k]) own[k] = from[k];
      });
      l.pools = Object.keys(own).length ? own : null;
    });
  }

  function setIds(e, key, text) {
    const ids = String(text).split(/[\s,]+/).filter(x => x).map(Number).filter(x => Number.isInteger(x) && x > 0);

    e[key] = ids.length ? ids : null;
  }

  // ===== Events =====

  // A reward just made: open it in a list that shows it, and scroll the list to it.
  function showMade(made) {
    if (st.view === 'days') st.view = 'file';

    st.q = '';

    select(made.id);
    render();
    showSelectedRow();
  }

  function onClick(ev) {
    const t = ev.target.closest('[data-a]');

    if (!t || (t.getAttribute('data-a') === 'sel' && ev.target.closest('.setting-toggle'))) return;

    const a = t.getAttribute('data-a');
    const v = t.getAttribute('data-v');
    const k = t.getAttribute('data-k');
    const c = Number(t.getAttribute('data-c'));
    const e = entryOf(st.sel);

    switch (a) {
      case 'view': st.view = v === 'all' || v === 'days' ? v : 'file'; closeMenus(); if (st.view !== 'days') keepSelectionInView(); break;
      case 'rqx': clearSearch(); return;
      case 'poolmenu': if (st.menu && st.menu.kind === v) closeMenus(); else openPoolMenu(t, v === 'day' ? 'day' : 'reward'); return;
      case 'pmpick': addPool(Number(v)); return;
      case 'day': st.day = Number(v); st.peek = 0; st.drawn = null; break;
      case 'peek': st.peek = st.peek === Number(v) ? 0 : Number(v); break;
      case 'dpup': case 'dpdn': moveDayPool(st.day, Number(v), a === 'dpup' ? -1 : 1); break;
      case 'rmdpool': setDayPools(st.day, dayPools(st.day).filter(p => p.pool !== Number(v))); break;
      case 'dayreset': delete st.dayEdits[st.day]; st.dirty = true; st.drawn = null; break;
      case 'draw': st.drawn = testDraw(st.day); break;
      case 'char': if (e) toggleChar(e, Number(v)); break;
      case 'new': {
        const made = newReward();

        if (made) { showMade(made); return; }

        toast(esc(L('There is no free reward number left.', '没有可用的奖励编号了。')));

        break;
      }
      case 'save': save(); return;
      case 'discard': closeOverlays(); discard(); return;
      case 'sel': select(Number(v)); clampLevel(); break;
      case 'lv': st.lv = Number(v); clampLevel(); break;
      case 'addlv': if (e && e.levels.length < MAX_LEVELS) { addLevel(e); st.lv = e.levels.length - 1; } break;
      case 'rmlv': if (e) { removeLevel(e); clampLevel(); } break;
      case 'rmpool': if (e) { removePool(selectedLevel(e), Number(v)); markChanged(e); } break;
      case 'poolsall': if (e) { poolsForAll(e); markChanged(e); } break;
      case 'pvl': st.pvLang = v === 'zh' ? 'zh' : 'en'; st.pvChosen = true; break;
      case 'fold': st.textOpen = !st.textOpen; break;
      case 'icon': if (st.iconsEl) closeIcons(); else openIcons(t); return;
      case 'icg': showIconGroup(v || REWARD_ICONS); return;
      case 'ic': {
        const ic = cat.icons[Number(v)];

        if (e && ic) {
          e.icon = ic.icon;
          e.unityIcon = ic.unityIcon;
          markChanged(e);
        }

        closeIcons();
        break;
      }
      case 'add': if (e && canEditGives(e) && e.gives.length < MAX_GIVES) openPicker('add', -1); return;
      case 'chg': if (e && canEditGives(e)) openPicker('replace', c); return;
      case 'rm': if (e && canEditGives(e) && (e.fresh || e.gives.length > 1)) { e.gives.splice(c, 1); givesChanged(e); } break;
      case 'reset': case 'delete': if (e) { resetOrDelete(e); afterRemove(); } break;
      case 'edit': adopt(st.sel); break;
      case 'copy': {
        const made = copyInto(st.sel);

        if (made) { showMade(made); return; }

        toast(esc(L('There is no free reward number left.', '没有可用的奖励编号了。')));

        break;
      }
      case 'dec': case 'inc':
        if (k && k.indexOf('d:') === 0) stepDay(k, a === 'inc' ? 1 : -1);
        else stepValue(e, k, a === 'inc' ? 1 : -1);

        return;
      case 'card': st.picker.selId = v; renderPicker(false); return;
      case 'src': st.picker.src = v; renderPicker(true); return;
      case 'cat': st.picker.cat = v; renderPicker(false); return;
      case 'close': closePicker(); return;
      case 'choose': choose(); return;
      default: return;
    }

    render();
  }

  // The id lists are written when you leave the field, so SAVE lights up while you type.
  function onTabInput(ev) {
    if (ev.target.getAttribute('data-a') === 'ids') refreshSaved(true);
    else onInput(ev);
  }

  function onChange(ev) {
    const t = ev.target;
    const a = t.getAttribute('data-a');
    const e = entryOf(st.sel);

    if (a === 'tog') {
      const target = entryOf(Number(t.getAttribute('data-v')));

      if (target) {
        target.enabled = t.checked;
        st.dirty = true;
      }
    } else if (a === 'pk' && e) {
      const g = e.gives[Number(t.getAttribute('data-c'))];
      let value;

      try { value = JSON.parse(t.value); } catch (err) { value = t.value; }

      if (g) setPick(e, g, t.getAttribute('data-k'), value);
    } else if (a === 'val' && String(t.getAttribute('data-k')).indexOf('d:') === 0) setDayAmount(st.day, Number(t.getAttribute('data-k').slice(2)), parseFloat(t.value));
    else if (a === 'reget' && e) {
      e.rules.isReget = t.checked;
      markChanged(e);
    } else if (a === 'rule' && e) {
      e.rules.endless = Number(t.value);
      markChanged(e);
    } else if (a === 'val' && e) {
      const target = stepTarget(e, t.getAttribute('data-k'));
      const value = parseFloat(t.value);

      if (target && isFinite(value)) writeValue(e, target, value);
    } else if (a === 'auto' && e) setAuto(e, t.checked);
    else if (a === 'ids' && e) {
      setIds(e, t.getAttribute('data-k') === 'furniture' ? 'furniture' : 'needs', t.value);
      markChanged(e);
    } else return;

    renderAfterChange();
  }

  // ===== Wiring =====

  function load() {
    if (st.asked) return;

    st.asked = true;

    if (window.vuplex) send('load');
    else window.addEventListener('vuplexready', () => send('load'), { once: true });
  }

  function showTab(el, ctx) {
    injectStyle();
    st.ctx = ctx || null;
    st.lang = ctx && ctx.lang === 'zh' ? 'zh' : 'en';

    if (!st.pvChosen) st.pvLang = st.lang;

    if (!st.root) {
      st.root = document.createElement('div');
      st.root.className = 'mr';
      st.root.addEventListener('click', onClick);
      st.root.addEventListener('change', onChange);
      st.root.addEventListener('input', onTabInput);
      st.root.addEventListener('keydown', onSearchKey);
    }

    if (st.root.parentNode !== el) el.appendChild(st.root);

    load();
    render();
    watchHeight();
    watchPresses();
  }

  function data(d) {
    if (!isObj(d)) return;

    st.data = d;
    st.game = isArr(d.game) ? d.game.filter(t => isObj(t) && isNum(t.id) && isArr(t.levels) && t.levels.length).map(gameReward) : [];
    st.byId = {};
    st.game.forEach(t => { st.byId[t.id] = t; });
    st.templates = isObj(d.templates) ? d.templates : {};
    st.scripts = new Set(Object.keys(st.templates));
    st.allPools = isArr(d.pools) ? d.pools.filter(isNum) : [];
    st.characters = isArr(d.characters) ? d.characters.filter(c => isObj(c) && isNum(c.id)).map(c => ({ id: c.id, name: normText(c.name) || { en: '#' + c.id, zh: '#' + c.id } })) : [];
    st.gameDays = isArr(d.days) ? d.days.filter(x => isObj(x) && isNum(x.day) && isArr(x.pools)).map(x => ({
      day: x.day,
      pools: x.pools.filter(p => isArr(p) && isNum(p[0]) && isNum(p[1])).map(p => ({ pool: p[0], amount: p[1] }))
    })) : [];

    if (!gameDay(st.day)) st.day = st.gameDays.length ? st.gameDays[0].day : 0;
    st.builtIn = {};
    (isObj(d.builtIn) && isArr(d.builtIn.rewards) ? d.builtIn.rewards : []).forEach(r => {
      if (isObj(r) && isNum(r.id) && !st.byId[r.id]) st.builtIn[r.id] = r;
    });
    st.icons = isArr(d.icons) ? d.icons.filter(i => isObj(i) && typeof i.icon === 'string' && i.icon).map(i => ({
      icon: i.icon,
      unityIcon: typeof i.unityIcon === 'string' ? i.unityIcon : '',
      group: typeof i.group === 'string' && i.group ? i.group : REWARD_ICONS
    })) : [];
    st.file = isObj(d.file) ? d.file : null;
    st.problems = isArr(d.problems) ? d.problems.map(String) : [];
    st.pending = null;
    closeOverlays();
    buildCatalogue();
    buildEntries(st.sel);
    render();
  }

  // The plugin sends each game reward with its levels; the tab reads the first level's name and text as the reward's.
  function gameReward(t) {
    const levels = t.levels.map(l => ({
      id: isNum(l.id) ? l.id : t.id,
      cost: isNum(l.cost) ? l.cost : 0,
      name: normText(l.name) || { en: '', zh: '' },
      desc: normText(l.desc),
      script: typeof l.script === 'string' ? l.script : '',
      values: isObj(l.values) ? l.values : {},
      pools: isObj(l.pools) ? poolsFrom(l.pools) : {}
    }));

    return {
      id: t.id,
      available: isNum(t.available) ? t.available : 1,
      icon: typeof t.icon === 'string' ? t.icon : '',
      unityIcon: typeof t.unityIcon === 'string' ? t.unityIcon : '',
      needs: isArr(t.needs) ? t.needs.filter(isNum) : [],
      furniture: isArr(t.furniture) ? t.furniture.filter(isNum) : [],
      name: levels[0].name,
      desc: levels[0].desc,
      rules: {
        isReget: t.isReget === true,
        endless: isNum(t.endless) ? t.endless : 0,
        chars: isArr(t.chars) ? t.chars.filter(isNum) : [],
        layers: isNum(t.layers) ? t.layers : 0,
        fromDay: isNum(t.fromDay) ? t.fromDay : 0
      },
      also: isArr(t.also) ? t.also.map(String) : [],
      levels
    };
  }

  // Level names are searched too, since a level's card can carry its own name.
  const searchExtra = e => range(e.levels.length).map(i => levelNameOf(e, i, 'en') + ' ' + levelNameOf(e, i, 'zh')).join(' ');

  Object.assign(T, {
    CSS, PREFIX, SCOPES, STYLE_ID, actionsHtml, allIds, buildEntries, canEditGives, closeMenus, detailHtml, editText,
    effectValues, firstTpl, footer, ghostOf, givesHtml, isAvailable, levelsHtml, lookHtml, makeGive, pickerHead,
    previewHtml, render, rowsHtml, searchExtra, srcTag, st, textHtml, toast, tplValues, whyHtml, writeGive
  });

  window.__slmrRewards = { data, saved };

  if (!isArr(window.__modmanagerTabs)) window.__modmanagerTabs = [];

  window.__modmanagerTabs.push({ guid: GUID, id: TAB_ID, title: TITLE, show: showTab, hide: hideTab, escape: escapeTab });

  try {
    if (window.__modmanager && typeof window.__modmanager.tabsChanged === 'function') window.__modmanager.tabsChanged();
  } catch (err) {}
})();
