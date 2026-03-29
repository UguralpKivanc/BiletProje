import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Özel metrikler
const errorRate = new Rate('hata_orani');
const eventsDuration = new Trend('events_sure_ms');
const ticketsDuration = new Trend('tickets_sure_ms');

export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Yavaş yüksel
    { duration: '1m',  target: 10 },   // Sabit yük
    { duration: '30s', target: 50 },   // Ani yük (spike)
    { duration: '30s', target: 10 },   // Spike sonrası
    { duration: '20s', target: 0  },   // Yavaş düşüş
  ],
  thresholds: {
    http_req_duration:      ['p(95)<500'],  // %95 istek < 500ms
    http_req_failed:        ['rate<0.05'],  // Hata oranı < %5
    hata_orani:             ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const API_KEY  = __ENV.API_KEY  || 'KingoSifre123';

const headers = {
  'X-Api-Key': API_KEY,
  'Content-Type': 'application/json',
};

// ── TEST SENARYOSU ─────────────────────────────────────────────────────────
export default function () {

  // 1) Login ile JWT al
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ Username: 'admin', Password: 'Bilet2026' }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  const loginOk = check(loginRes, {
    'Login 200': (r) => r.status === 200,
    'Token var': (r) => r.json('token') !== undefined,
  });
  errorRate.add(!loginOk);

  let jwtHeaders = { ...headers };
  if (loginOk) {
    jwtHeaders['Authorization'] = `Bearer ${loginRes.json('token')}`;
  }

  sleep(0.3);

  // 2) Etkinlikleri listele (X-Api-Key ile)
  const eventsRes = http.get(`${BASE_URL}/api/events`, { headers });
  const eventsOk = check(eventsRes, {
    'GET /api/events - 200': (r) => r.status === 200,
    'GET /api/events - içerik var': (r) => r.body.length > 0,
  });
  errorRate.add(!eventsOk);
  eventsDuration.add(eventsRes.timings.duration);

  sleep(0.3);

  // 3) Biletleri listele (JWT ile)
  const ticketsRes = http.get(`${BASE_URL}/api/tickets`, { headers: jwtHeaders });
  const ticketsOk = check(ticketsRes, {
    'GET /api/tickets - 200': (r) => r.status === 200,
  });
  errorRate.add(!ticketsOk);
  ticketsDuration.add(ticketsRes.timings.duration);

  sleep(0.3);

  // 4) Yeni bilet oluştur
  const newTicket = {
    eventName: 'Tarkan Konseri',
    customerName: `k6-test-kullanici-${__VU}`,
    seat: `K6-${__VU}-${__ITER}`,
    status: 'Active',
    price: 500,
  };

  const postRes = http.post(
    `${BASE_URL}/api/tickets`,
    JSON.stringify(newTicket),
    { headers: jwtHeaders }
  );
  check(postRes, {
    'POST /api/tickets - 201': (r) => r.status === 201,
  });

  sleep(1);
}

// ── ÖZET RAPORU ────────────────────────────────────────────────────────────
export function handleSummary(data) {
  return {
    'k6/rapor.json': JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: '  ', enableColors: true }),
  };
}

function textSummary(data, opts) {
  return `
=== BİLET SİSTEMİ YÜK TESTİ SONUÇLARI ===
Toplam İstek  : ${data.metrics.http_reqs?.values?.count ?? 0}
Hata Oranı    : ${((data.metrics.http_req_failed?.values?.rate ?? 0) * 100).toFixed(2)}%
Ort. Süre     : ${(data.metrics.http_req_duration?.values?.avg ?? 0).toFixed(2)}ms
P95 Süre      : ${(data.metrics.http_req_duration?.values?.['p(95)'] ?? 0).toFixed(2)}ms
Max Süre      : ${(data.metrics.http_req_duration?.values?.max ?? 0).toFixed(2)}ms
==========================================
`;
}
