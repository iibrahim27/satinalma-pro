export interface DeepLinkParams {
  module: string;
  screen: string;
  action: string;
  entityType: string;
  entityId: string;
  tab?: string;
  eventCode?: string;
}

export function buildDeepLinkUri(p: DeepLinkParams): string {
  const q = new URLSearchParams({
    module: p.module,
    screen: p.screen,
    action: p.action,
    entityType: p.entityType,
    entityId: p.entityId,
  });
  if (p.tab) q.set("tab", p.tab);
  if (p.eventCode) q.set("eventCode", p.eventCode);
  return `metrik://${p.module}/${p.screen}?${q.toString()}`;
}

export function buildDesktopRoute(p: DeepLinkParams): string {
  const mod = p.module === "satinalma" ? "Satınalma" : p.module;
  return `${mod}|${p.screen}|${p.entityId}`;
}
