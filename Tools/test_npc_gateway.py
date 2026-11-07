"""Offline tests for Tools/NpcGateway.py. Run: python3 -m unittest Tools/test_npc_gateway.py
No test reaches the network: provider calls are stubbed or sent to a loopback fake upstream."""
import http.client
import json
import os
import socket
import sys
import tempfile
import threading
import time
import unittest
import urllib.error
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parent))
import NpcGateway as gw  # noqa: E402

NO_PROXY = {k: v for k, v in os.environ.items() if not k.lower().endswith("_proxy")}


def unity_payload(**extra):
    data = {"name": "윤", "occupation": "간호사", "personality": "차분함", "context": "야간 근무", "place": "온유병원",
            "messages": [{"role": "assistant", "content": "서하 씨, 몸은 괜찮으세요?"}, {"role": "user", "content": "일 좀 있어요?"}],
            "day": 3, "wanted": 0, "site": -1, "hour": 21.5, "allowQuest": True, "opening": False,
            "sites": [gw.fallback_site(i) for i in range(gw.SITE_COUNT)]}
    data.update(extra)
    return data


class Settings(unittest.TestCase):
    def test_model_default_env_and_env_file(self):
        with tempfile.TemporaryDirectory() as folder:
            env = Path(folder) / ".env.local"
            with mock.patch.dict(os.environ, {"NPC_MODEL": "", "OPENAI_API_KEY": ""}):
                self.assertEqual(gw.load_model(env), gw.DEFAULT_MODEL)
                env.write_text('OPENAI_API_KEY="sk-file"\nNPC_MODEL=gpt-test-mini\n', encoding="utf-8")
                self.assertEqual(gw.load_model(env), "gpt-test-mini")
                self.assertEqual(gw.load_key(env), "sk-file")
            with mock.patch.dict(os.environ, {"NPC_MODEL": "gpt-env"}):
                self.assertEqual(gw.load_model(env), "gpt-env")
            with mock.patch.dict(os.environ, {"NPC_MODEL": "bad model; rm -rf"}):
                self.assertEqual(gw.load_model(env), gw.DEFAULT_MODEL)
        self.assertEqual(gw.DEFAULT_MODEL, "gpt-5.6-luna")

    def test_build_request_uses_model(self):
        self.assertEqual(gw.build_request(unity_payload(), "gpt-x")["model"], "gpt-x")


class Sites(unittest.TestCase):
    def test_fallback_table_matches_urban_catalog(self):
        self.assertEqual(len(gw.FALLBACK_SITES), 40)
        self.assertEqual(gw.FALLBACK_SITES[0], "새벽아파트")
        self.assertEqual(gw.FALLBACK_SITES[17], "동부경찰서 · 2구역")
        self.assertEqual(gw.FALLBACK_SITES[32], "애프터뷰 호텔")
        self.assertEqual(gw.FALLBACK_SITES[38], "애프터라이트 모터랩")
        self.assertEqual(gw.FALLBACK_SITES[39], "NEON AFTERHOURS BAR")

    def test_client_names_win_and_are_cleaned(self):
        names = gw.site_names({"sites": ["<b>A</b>", "", 7, "x;y\nz"]})
        self.assertEqual(names[:4], ["A", gw.FALLBACK_SITES[1], gw.FALLBACK_SITES[2], "x,y z"])
        self.assertEqual(names[4:], gw.FALLBACK_SITES[4:])
        self.assertEqual(gw.site_names({}), gw.FALLBACK_SITES)
        self.assertEqual(gw.site_names({"sites": "nope"}), gw.FALLBACK_SITES)

    def test_instructions_list_client_sites(self):
        sites = [f"장소{i}" for i in range(40)]
        text = gw.build_request(unity_payload(sites=sites))["instructions"]
        self.assertIn("38: 장소38", text)
        self.assertIn("39: 장소39", text)
        self.assertNotIn("오름백화점", text)
        text = gw.build_request(unity_payload(sites=None))["instructions"]
        self.assertIn("38: 애프터라이트 모터랩", text)


class Requests(unittest.TestCase):
    def test_validation(self):
        self.assertTrue(gw.validate(unity_payload()))
        self.assertTrue(gw.validate({"messages": []}))
        self.assertTrue(gw.validate({}))
        self.assertFalse(gw.validate([]))
        self.assertFalse(gw.validate({"messages": "hello"}))
        self.assertFalse(gw.validate({"messages": [{"role": "user", "content": 5}]}))
        self.assertFalse(gw.validate({"messages": ["text"]}))
        self.assertFalse(gw.validate({"messages": [{"content": "x"}] * 65}))
        self.assertFalse(gw.validate({"sites": "a"}))
        self.assertFalse(gw.validate({"sites": ["a"] * 41}))

    def test_messages_are_bounded_and_roles_normalized(self):
        messages = [{"role": "system", "content": "x" * 3000}] + [{"role": "user", "content": str(i)} for i in range(10)]
        built = gw.build_request({"messages": messages})["input"]
        self.assertEqual(len(built), 8)
        self.assertTrue(all(m["role"] in ("user", "assistant") for m in built))
        built = gw.build_request({"messages": [{"role": "system", "content": "x" * 3000}]})["input"]
        self.assertEqual(built[0]["role"], "user")
        self.assertEqual(len(built[0]["content"]), 1500)

    def test_opening_turn_only_without_history(self):
        request = gw.build_request({"messages": []})
        self.assertEqual(request["input"][0]["role"], "developer")
        self.assertIn("opening turn", request["instructions"])
        self.assertNotIn("opening turn", gw.build_request(unity_payload())["instructions"])


class Replies(unittest.TestCase):
    def quest(self, **values):
        quest = {"title": "약품 상자 전달", "description": "소방서로 전달", "kind": "delivery", "site": 2, "reward": 180}
        quest.update(values)
        return gw.finish_reply({"reply": "부탁 하나 할게요.", "quest": quest}, {"allowQuest": True})["quest"]

    def test_reward_clamped_120_350(self):
        self.assertEqual(self.quest(reward=50)["reward"], 120)
        self.assertEqual(self.quest(reward=9999)["reward"], 350)
        self.assertEqual(self.quest(reward=200)["reward"], 200)
        self.assertEqual(self.quest(reward="many")["reward"], 120)
        self.assertEqual(self.quest(reward=None)["reward"], 120)

    def test_invalid_quests_dropped(self):
        self.assertIsNone(self.quest(site=40))
        self.assertIsNone(self.quest(site=-1))
        self.assertIsNone(self.quest(site=True))
        self.assertIsNone(self.quest(site="2"))
        self.assertIsNone(self.quest(kind="assassinate"))
        self.assertIsNone(self.quest(title="<b></b>"))
        self.assertEqual(self.quest(site=39)["site"], 39)
        reply = {"reply": "안녕하세요.", "quest": {"title": "t", "description": "d", "kind": "visit", "site": 1, "reward": 200}}
        self.assertIsNone(gw.finish_reply(reply, {"allowQuest": False})["quest"])

    def test_quest_text_bounded(self):
        quest = self.quest(title="가" * 100, description="나" * 400)
        self.assertEqual(len(quest["title"]), 64)
        self.assertEqual(len(quest["description"]), 180)

    def test_reply_cleaned_and_bounded(self):
        reply = gw.finish_reply({"reply": "<color=red>위험</color>해요\u0007‮ <<b>b>" + "가" * 2000, "quest": None}, {})["reply"]
        self.assertNotIn("<", reply)
        self.assertNotIn("\u0007", reply)
        self.assertNotIn("‮", reply)
        self.assertTrue(reply.startswith("위험해요 ＜b＞"))
        self.assertEqual(len(reply), 1300)
        self.assertEqual(gw.clean_text("3 < 5 and 7 > 2 <size=90>크게</size>", 99), "3 ＜ 5 and 7 ＞ 2 크게")
        for bad in ({"reply": "  "}, {"reply": None}, {"reply": "<i></i>"}, [], {"quest": None}):
            with self.assertRaises(ValueError):
                gw.finish_reply(bad, {})


class GatewayServer(unittest.TestCase):
    """Real loopback server; the provider call is stubbed per test."""

    def setUp(self):
        self.env = mock.patch.dict(os.environ, NO_PROXY, clear=True)
        self.env.start()
        gw.LAST_REQUEST = 0.0
        gw.GATE = gw.threading.BoundedSemaphore(2)
        gw.Handler.key, gw.Handler.model = "test-key", "gpt-test"
        self.server = gw.make_server(0)
        self.port = self.server.server_address[1]
        threading.Thread(target=self.server.serve_forever, daemon=True).start()

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        gw.Handler.key, gw.Handler.model = "", gw.DEFAULT_MODEL
        self.env.stop()

    def call(self, body=None, headers=None, method="POST", path="/dialogue"):
        connection = http.client.HTTPConnection("127.0.0.1", self.port, timeout=10)
        payload = body if isinstance(body, bytes) else json.dumps(unity_payload() if body is None else body).encode()
        connection.request(method, path, body=payload if method == "POST" else None, headers=headers or {"Content-Type": "application/json"})
        response = connection.getresponse()
        result = response.status, json.loads(response.read() or b"{}"), response.getheader("Retry-After")
        connection.close()
        return result

    def test_loopback_bind_and_health(self):
        self.assertEqual(self.server.server_address[0], "127.0.0.1")
        code, body, _ = self.call(method="GET", path="/health")
        self.assertEqual((code, body), (200, {"ready": True, "model": "gpt-test", "service": "aftersignal-npc"}))

    def test_rejections(self):
        self.assertEqual(self.call(headers={"Content-Type": "application/json", "Origin": "https://evil.example"})[:2], (403, {"error": "unsupported_client"}))
        self.assertEqual(self.call(headers={"Content-Type": "text/plain"})[:2], (403, {"error": "unsupported_client"}))
        self.assertEqual(self.call(body=b"{not json")[:2], (400, {"error": "invalid_request"}))
        self.assertEqual(self.call(body={"messages": "x"})[:2], (400, {"error": "invalid_request"}))
        self.assertEqual(self.call(body=b"[]")[:2], (400, {"error": "invalid_request"}))
        self.assertEqual(self.call(body=b"{}", headers={"Content-Type": "application/json", "Content-Length": str(gw.MAX_BODY + 1)})[:2], (413, {"error": "request_too_large"}))
        self.assertEqual(self.call(path="/other")[:2], (404, {"error": "not_found"}))

    def test_not_configured_returns_in_character_reply(self):
        gw.Handler.key = ""
        code, body, _ = self.call()
        self.assertEqual(code, 503)
        self.assertEqual(body["error"], "not_configured")
        self.assertTrue(body["reply"].strip())
        self.assertTrue(body["status"])
        self.assertIsNone(body["quest"])

    def test_success_and_rate_limit(self):
        with mock.patch.object(gw, "generate", return_value={"reply": "반가워요.", "quest": None}) as generate:
            code, body, _ = self.call()
            self.assertEqual((code, body), (200, {"reply": "반가워요.", "quest": None}))
            self.assertEqual(generate.call_args[0][2], "gpt-test")
            code, body, retry = self.call()
            self.assertEqual(code, 429)
            self.assertEqual(body["error"], "please_wait")
            self.assertTrue(0 < body["retry_after"] <= gw.MIN_INTERVAL)
            self.assertEqual(retry, "1")
            self.assertEqual(generate.call_count, 1)

    def test_busy_when_two_generations_run(self):
        gw.GATE.acquire()
        gw.GATE.acquire()
        try:
            code, body, retry = self.call()
        finally:
            gw.GATE.release()
            gw.GATE.release()
        self.assertEqual((code, body["error"], retry), (429, "busy", "1"))
        self.assertGreater(body["retry_after"], 0)

    def test_provider_errors(self):
        error = urllib.error.HTTPError(gw.API_URL, 429, "Too Many Requests", {}, None)
        with mock.patch.object(gw, "generate", side_effect=error):
            self.assertEqual(self.call()[:2], (502, {"error": "provider_unavailable", "provider_status": 429}))
        gw.LAST_REQUEST = 0.0
        with mock.patch.object(gw, "generate", side_effect=urllib.error.URLError(socket.timeout("timed out"))):
            self.assertEqual(self.call()[:2], (504, {"error": "provider_timeout"}))
        gw.LAST_REQUEST = 0.0
        with mock.patch.object(gw, "generate", side_effect=ValueError("Incomplete response")):
            self.assertEqual(self.call()[:2], (502, {"error": "dialogue_unavailable"}))

    def test_hard_deadline(self):
        def slow(*_):
            time.sleep(1)
            return {"reply": "늦었어요.", "quest": None}
        with mock.patch.object(gw, "UPSTREAM_TIMEOUT", -0.9), mock.patch.object(gw, "generate", side_effect=slow):
            self.assertEqual(self.call()[:2], (504, {"error": "provider_timeout"}))


class SlowUpstream(BaseHTTPRequestHandler):
    """Fake provider that never answers; records when the gateway closes the connection."""
    received = threading.Event()
    closed = threading.Event()

    def log_message(self, *_):
        pass

    def do_POST(self):
        self.rfile.read(int(self.headers.get("Content-Length", "0")))
        SlowUpstream.received.set()
        self.connection.settimeout(8)
        try:
            if self.connection.recv(1) == b"":
                SlowUpstream.closed.set()
        except OSError:
            SlowUpstream.closed.set()


class ClientDisconnect(unittest.TestCase):
    def test_disconnect_closes_upstream_and_frees_slot(self):
        env = mock.patch.dict(os.environ, NO_PROXY, clear=True)
        env.start()
        upstream = ThreadingHTTPServer(("127.0.0.1", 0), SlowUpstream)
        upstream.daemon_threads = True
        threading.Thread(target=upstream.serve_forever, daemon=True).start()
        gw.LAST_REQUEST = 0.0
        gw.GATE = gw.threading.BoundedSemaphore(2)
        gw.Handler.key = "test-key"
        server = gw.make_server(0)
        threading.Thread(target=server.serve_forever, daemon=True).start()
        try:
            with mock.patch.object(gw, "API_URL", f"http://127.0.0.1:{upstream.server_address[1]}/v1/responses"):
                body = json.dumps(unity_payload()).encode()
                client = socket.create_connection(("127.0.0.1", server.server_address[1]), timeout=5)
                client.sendall(b"POST /dialogue HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Type: application/json\r\nContent-Length: " + str(len(body)).encode() + b"\r\n\r\n" + body)
                self.assertTrue(SlowUpstream.received.wait(5), "gateway forwarded the request upstream")
                client.close()  # What UnityWebRequest.Abort does when the player cancels.
                self.assertTrue(SlowUpstream.closed.wait(3), "gateway closed the upstream connection after the game disconnected")
                deadline = time.monotonic() + 3
                while gw.GATE._value < 2 and time.monotonic() < deadline:
                    time.sleep(.05)
                self.assertEqual(gw.GATE._value, 2, "generation slot released")
        finally:
            server.shutdown()
            server.server_close()
            upstream.shutdown()
            upstream.server_close()
            gw.Handler.key = ""
            env.stop()


if __name__ == "__main__":
    unittest.main()
