export interface NotificationTemplate {
  eventCode: string;
  category: string;
  type: string;
  defaultPriority: string;
  titleTemplate: string;
  messageTemplate: string;
  targetRoles: string[];
  module: string;
  screen: string;
  action: string;
  enabled: boolean;
}

export const NOTIFICATION_TEMPLATES: Record<string, NotificationTemplate> = {
  "talep.olusturuldu": {
    eventCode: "talep.olusturuldu",
    category: "talep",
    type: "INFO",
    defaultPriority: "MEDIUM",
    titleTemplate: "Yeni Talep",
    messageTemplate: "{talepNo} — {talepEden}",
    targetRoles: ["Yönetim", "Satınalma", "Admin"],
    module: "satinalma",
    screen: "gelen-talepler",
    action: "view",
    enabled: true,
  },
  "talep.yonetime_gonderildi": {
    eventCode: "talep.yonetime_gonderildi",
    category: "talep",
    type: "APPROVAL",
    defaultPriority: "HIGH",
    titleTemplate: "Yönetime Gönderildi",
    messageTemplate: "{talepNo} yönetim incelemesinde",
    targetRoles: ["Yönetim", "Satınalma", "Admin"],
    module: "satinalma",
    screen: "gelen-talepler",
    action: "review",
    enabled: true,
  },
  "teklif.istendi": {
    eventCode: "teklif.istendi",
    category: "teklif",
    type: "TASK",
    defaultPriority: "HIGH",
    titleTemplate: "Teklif İstendi",
    messageTemplate: "{talepNo} için teklif toplanacak",
    targetRoles: ["Satınalma", "Admin"],
    module: "satinalma",
    screen: "teklif-giris",
    action: "add_quote",
    enabled: true,
  },
  "teklif.yonetime_gonderildi": {
    eventCode: "teklif.yonetime_gonderildi",
    category: "teklif",
    type: "APPROVAL",
    defaultPriority: "HIGH",
    titleTemplate: "Teklif Onayda",
    messageTemplate: "{talepNo} teklifleri onayınızı bekliyor",
    targetRoles: ["Yönetim", "Admin"],
    module: "satinalma",
    screen: "teklif-onay",
    action: "compare",
    enabled: true,
  },
  "teklif.duzeltme_istendi": {
    eventCode: "teklif.duzeltme_istendi",
    category: "teklif",
    type: "TASK",
    defaultPriority: "HIGH",
    titleTemplate: "Teklif Düzeltme",
    messageTemplate: "{talepNo} teklifleri düzeltilmeli",
    targetRoles: ["Satınalma", "Admin"],
    module: "satinalma",
    screen: "teklif-giris",
    action: "edit_quote",
    enabled: true,
  },
  "talep.onaylandi": {
    eventCode: "talep.onaylandi",
    category: "talep",
    type: "INFO",
    defaultPriority: "MEDIUM",
    titleTemplate: "Talep Onaylandı",
    messageTemplate: "{talepNo} onaylandı",
    targetRoles: ["Satınalma", "Admin"],
    module: "satinalma",
    screen: "onaylanan-talepler",
    action: "view",
    enabled: true,
  },
  "talep.reddedildi": {
    eventCode: "talep.reddedildi",
    category: "talep",
    type: "WARNING",
    defaultPriority: "HIGH",
    titleTemplate: "Talep Reddedildi",
    messageTemplate: "{talepNo} reddedildi",
    targetRoles: [],
    module: "satinalma",
    screen: "talep-detay",
    action: "view",
    enabled: true,
  },
  "siparis.olusturuldu": {
    eventCode: "siparis.olusturuldu",
    category: "siparis",
    type: "INFO",
    defaultPriority: "HIGH",
    titleTemplate: "Sipariş Oluşturuldu",
    messageTemplate: "{siparisNo} — {talepNo}",
    targetRoles: ["Depo", "Satınalma", "Admin"],
    module: "satinalma",
    screen: "siparis-detay",
    action: "view",
    enabled: true,
  },
  "depo.mal_kabul_yapildi": {
    eventCode: "depo.mal_kabul_yapildi",
    category: "depo",
    type: "INFO",
    defaultPriority: "MEDIUM",
    titleTemplate: "Mal Kabul",
    messageTemplate: "{talepNo} mal kabul yapıldı",
    targetRoles: ["Satınalma", "Admin"],
    module: "depo",
    screen: "mal-kabul",
    action: "receive",
    enabled: true,
  },
  "talep.sla_yaklasiyor": {
    eventCode: "talep.sla_yaklasiyor",
    category: "talep",
    type: "REMINDER",
    defaultPriority: "HIGH",
    titleTemplate: "Onay SLA Yaklaşıyor",
    messageTemplate: "{talepNo} 24 saatten fazla onay bekliyor",
    targetRoles: ["Yönetim", "Admin"],
    module: "satinalma",
    screen: "teklif-onay",
    action: "approve",
    enabled: true,
  },
  "talep.sla_asildi": {
    eventCode: "talep.sla_asildi",
    category: "talep",
    type: "URGENT",
    defaultPriority: "CRITICAL",
    titleTemplate: "Onay SLA Aşıldı",
    messageTemplate: "{talepNo} acil onay gerekiyor",
    targetRoles: ["Yönetim", "Satınalma", "Admin"],
    module: "satinalma",
    screen: "gelen-talepler",
    action: "approve",
    enabled: true,
  },
};

export const LEGACY_TIP_TO_EVENT: Record<string, string> = {
  yonetime_gonderildi: "talep.yonetime_gonderildi",
  teklif_istendi: "teklif.istendi",
  teklif_onayda: "teklif.yonetime_gonderildi",
  teklif_duzeltme_istendi: "teklif.duzeltme_istendi",
  onaylandi: "talep.onaylandi",
  reddedildi: "talep.reddedildi",
  siparis_olusturuldu: "siparis.olusturuldu",
  mal_kabul_edildi: "depo.mal_kabul_yapildi",
};

export function statusTransitionEvent(before: string | null, after: string): string | null {
  if (!before) {
    // İlk kayıt doğrudan onaya gittiyse (Taslak/Hazırlanıyor atlandı) yönetim bildirimi üret.
    if (after === "İmza Sürecinde" || after === "Yönetim Onayında") {
      return "talep.yonetime_gonderildi";
    }
    return "talep.olusturuldu";
  }
  if (before === after) return null;

  const key = `${before}|${after}`;
  const map: Record<string, string> = {
    "Taslak|İmza Sürecinde": "talep.yonetime_gonderildi",
    "Hazırlanıyor|İmza Sürecinde": "talep.yonetime_gonderildi",
    "Taslak|Yönetim Onayında": "talep.yonetime_gonderildi",
    "Hazırlanıyor|Yönetim Onayında": "talep.yonetime_gonderildi",
    "İmza Sürecinde|Teklif Girişi": "teklif.istendi",
    "Yönetim Onayında|Teklif Girişi": "teklif.istendi",
    // Satınalma teklifleri yönetime iletti — en sık yol Teklif Girişi→Yönetim Onayında
    // (Karşılaştırma ara durumu tek yazımda atlanabiliyor).
    "Teklif Girişi|Yönetim Onayında": "teklif.yonetime_gonderildi",
    "Karşılaştırma|Yönetim Onayında": "teklif.yonetime_gonderildi",
    "İmza Sürecinde|Yönetim Onayında": "teklif.yonetime_gonderildi",
    "Yönetim Onayında|Karşılaştırma": "teklif.duzeltme_istendi",
    "Yönetim Onayında|Onaylandı": "talep.onaylandi",
    "Onaylandı|Sipariş Oluşturuldu": "siparis.olusturuldu",
  };

  if (map[key]) return map[key];
  if (after === "Reddedildi") return "talep.reddedildi";
  return null;
}

export function interpolate(template: string, vars: Record<string, string>): string {
  return template.replace(/\{(\w+)\}/g, (_, k) => vars[k] ?? "");
}
