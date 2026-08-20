import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { rewardApi } from '../../services/rewardApi.js';

const emptyProfile = {
  citizenName: 'CivicHero Citizen',
  points: 0,
  tier: 'Citizen',
  rank: '—',
  closedComplaints: 0,
  submittedComplaints: 0,
  helpfulVerifications: 0,
  supportedIssues: 0,
  followerCount: 0,
  badges: [],
};

export default function Leaderboard() {
  const [rows, setRows] = useState([]);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [selectedSummary, setSelectedSummary] = useState(null);
  const [selectedProfile, setSelectedProfile] = useState(null);
  const [profileOpen, setProfileOpen] = useState(false);
  const [profileLoading, setProfileLoading] = useState(false);
  const [followLoading, setFollowLoading] = useState(false);
  const [timeframe, setTimeframe] = useState('AllTime');

  useEffect(() => {
    let active = true;

    rewardApi.leaderboard(50, timeframe)
      .then(async (data) => {
        if (!active) return;
        const items = Array.isArray(data) ? data : data?.items || [];
        setRows(items);

        if (items[0]) {
          setSelectedSummary(items[0]);
          try {
            const profile = await rewardApi.leaderboardCitizenProfile(items[0].userId);
            if (active) setSelectedProfile(profile);
          }
          catch {
            // The list remains usable even if the optional preview request fails.
          }
        }
      })
      .catch((reason) => {
        if (active) setError(reason.message || 'The leaderboard could not be loaded.');
      });

    return () => { active = false; };
  }, [timeframe]);

  useEffect(() => {
    if (!profileOpen) return undefined;

    const closeOnEscape = (event) => {
      if (event.key === 'Escape') setProfileOpen(false);
    };

    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [profileOpen]);

  const top = useMemo(() => rows.slice(0, 3), [rows]);
  const preview = selectedProfile || selectedSummary || top[0] || emptyProfile;

  const loadProfile = async (row, openDialog = true) => {
    if (!row?.userId) return;

    setError('');
    setNotice('');
    setSelectedSummary(row);
    setSelectedProfile((current) => current?.userId === row.userId ? current : null);
    if (openDialog) setProfileOpen(true);
    setProfileLoading(true);

    try {
      const profile = await rewardApi.leaderboardCitizenProfile(row.userId);
      setSelectedProfile(profile);
    }
    catch (reason) {
      setError(reason.message || 'The citizen profile could not be loaded.');
    }
    finally {
      setProfileLoading(false);
    }
  };

  const toggleFollow = async () => {
    const userId = selectedProfile?.userId || selectedSummary?.userId;
    if (!userId || selectedProfile?.isCurrentUser) return;

    setError('');
    setNotice('');
    setFollowLoading(true);

    try {
      const updated = selectedProfile?.isFollowing
        ? await rewardApi.unfollowCitizen(userId)
        : await rewardApi.followCitizen(userId);

      setSelectedProfile(updated);
      setNotice(updated.isFollowing
        ? `You are now following ${updated.citizenName}.`
        : `You stopped following ${updated.citizenName}.`);
    }
    catch (reason) {
      setError(reason.message || 'The follow action could not be completed.');
    }
    finally {
      setFollowLoading(false);
    }
  };

  const openCurrentPreview = () => {
    const row = rows.find((item) => item.userId === preview.userId) || selectedSummary;
    if (row) loadProfile(row, true);
  };

  return (
    <section className="page-wrap">
      <div className="page-title-row">
        <div>
          <p className="section-kicker">Positive civic competition</p>
          <h2>Community leaderboard</h2>
          <p>Top contributors improving the city through valid complaints, verification and helpful participation.</p>
        </div>
        <Link to="/citizen" className="button outline">← Back to dashboard</Link>
      </div>

      {error && <div className="alert error" role="alert">{error}</div>}
      {notice && <div className="alert success" role="status">{notice}</div>}

      <div className="filter-bar">
        <label className="field" style={{ minWidth: 240 }}>
          <span>Leaderboard period</span>
          <select className="input" value={timeframe} onChange={(event) => setTimeframe(event.target.value)}>
            <option value="Weekly">This week</option>
            <option value="Monthly">This month</option>
            <option value="Yearly">This year</option>
            <option value="AllTime">All time</option>
          </select>
        </label>
        <div className="alert info" style={{ margin: 0, flex: 1 }}>
          Rankings and closed-issue counts are calculated only for the selected period.
        </div>
      </div>

      <div className="dashboard-grid main-aside section-gap">
        <div>
          <section className="surface">
            <div className="surface-header"><h3>Top 3 citizens</h3><span className="status-pill amber">★ Community heroes</span></div>
            <div className="surface-body">
              <div className="podium">
                {[top[1], top[0], top[2]].map((row, index) => (
                  <Podium
                    key={row?.userId || index}
                    row={row}
                    first={index === 1}
                    fallbackRank={index === 1 ? 1 : index === 0 ? 2 : 3}
                    onOpen={() => row && loadProfile(row, true)}
                  />
                ))}
              </div>
            </div>
          </section>

          <section className="surface section-gap">
            <div className="surface-header"><h3>Leaderboard list</h3><span className="muted" style={{ fontSize: 8 }}>Only valid closed complaints earn points</span></div>
            <div className="civic-table-wrap">
              <table className="civic-table">
                <thead><tr><th>Rank</th><th>Citizen</th><th>Tier</th><th>Closed issues</th><th>Points</th><th>Action</th></tr></thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.userId} style={{ background: row.isCurrentUser ? '#eef5ff' : undefined }}>
                      <td><strong>#{row.rank}</strong></td>
                      <td><strong>{row.citizenName}</strong>{row.isCurrentUser && <span className="status-pill" style={{ marginLeft: 7 }}>You</span>}</td>
                      <td>{row.tier}</td>
                      <td>{row.closedComplaints}</td>
                      <td><strong>{row.points}</strong></td>
                      <td><button className="button outline small" type="button" onClick={() => loadProfile(row, true)}>View profile</button></td>
                    </tr>
                  ))}
                  {rows.length === 0 && <tr><td colSpan="6">Leaderboard data will appear after citizens earn points.</td></tr>}
                </tbody>
              </table>
            </div>
          </section>
        </div>

        <aside className="dashboard-grid">
          <CitizenPreview
            profile={preview}
            loading={profileLoading}
            followLoading={followLoading}
            onFollow={toggleFollow}
            onOpen={openCurrentPreview}
          />

          <section className="surface">
            <div className="surface-header"><h3>How points work</h3></div>
            <div className="surface-body notification-list">
              <Notice title="Valid complaint" text="+10 points" />
              <Notice title="Verified closure" text="+20 points" />
              <Notice title="Support an issue" text="+2 points" />
              <Notice title="Helpful comment" text="+3 points" />
            </div>
          </section>
        </aside>
      </div>

      {profileOpen && (
        <div className="leaderboard-profile-overlay" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && setProfileOpen(false)}>
          <section className="leaderboard-profile-dialog" role="dialog" aria-modal="true" aria-labelledby="leaderboard-profile-title">
            <button className="leaderboard-profile-close" type="button" aria-label="Close citizen profile" onClick={() => setProfileOpen(false)}>×</button>
            {profileLoading && !selectedProfile
              ? <div className="leaderboard-profile-loading">Loading citizen profile…</div>
              : <CitizenProfileDetails profile={selectedProfile || preview} followLoading={followLoading} onFollow={toggleFollow} />}
          </section>
        </div>
      )}
    </section>
  );
}

function CitizenPreview({ profile, loading, followLoading, onFollow, onOpen }) {
  return (
    <section className="surface">
      <div className="surface-header"><h3>Citizen profile preview</h3></div>
      <div className="surface-body" style={{ textAlign: 'center' }}>
        <div className="podium-avatar">{initials(profile.citizenName)}</div>
        <h3 style={{ marginBottom: 4 }}>{profile.citizenName}</h3>
        <p className="muted" style={{ marginTop: 0, fontSize: 9 }}>{profile.points ?? '—'} points · {profile.tier || 'Citizen'}</p>
        <div className="leaderboard-profile-mini-stats">
          <span><strong>#{profile.rank ?? '—'}</strong><small>Rank</small></span>
          <span><strong>{profile.closedComplaints ?? 0}</strong><small>Closed</small></span>
          <span><strong>{profile.followerCount ?? 0}</strong><small>Followers</small></span>
        </div>
        <div className="leaderboard-badge-list">
          {(profile.badges?.length ? profile.badges : ['Active citizen']).slice(0, 3).map((badge) => <span key={badge} className="status-pill amber">★ {badge}</span>)}
        </div>
        <button className="button primary full" type="button" onClick={onFollow} disabled={loading || followLoading || profile.isCurrentUser || !profile.userId}>
          {profile.isCurrentUser ? 'This is your profile' : followLoading ? 'Updating…' : profile.isFollowing ? 'Following · Unfollow' : 'Follow citizen'}
        </button>
        <button className="button outline full" type="button" style={{ marginTop: 8 }} onClick={onOpen} disabled={loading || !profile.userId}>
          {loading ? 'Loading profile…' : 'View profile'}
        </button>
      </div>
    </section>
  );
}

function CitizenProfileDetails({ profile, followLoading, onFollow }) {
  const memberSince = profile.memberSince
    ? new Intl.DateTimeFormat('en-IN', { month: 'short', year: 'numeric' }).format(new Date(profile.memberSince))
    : 'Not available';

  return (
    <div className="leaderboard-profile-content">
      <div className="leaderboard-profile-heading">
        <div className="podium-avatar">{initials(profile.citizenName)}</div>
        <div>
          <p className="section-kicker">Public civic profile</p>
          <h2 id="leaderboard-profile-title">{profile.citizenName}</h2>
          <p>{profile.tier} · Member since {memberSince}</p>
        </div>
      </div>

      <div className="leaderboard-profile-stats">
        <ProfileStat label="Rank" value={`#${profile.rank ?? '—'}`} />
        <ProfileStat label="Points" value={profile.points ?? 0} />
        <ProfileStat label="Followers" value={profile.followerCount ?? 0} />
        <ProfileStat label="Complaints filed" value={profile.submittedComplaints ?? 0} />
        <ProfileStat label="Issues closed" value={profile.closedComplaints ?? 0} />
        <ProfileStat label="Helpful verifications" value={profile.helpfulVerifications ?? 0} />
        <ProfileStat label="Issues supported" value={profile.supportedIssues ?? 0} />
      </div>

      <div className="leaderboard-profile-badges">
        <h3>Community badges</h3>
        <div className="leaderboard-badge-list">
          {(profile.badges?.length ? profile.badges : ['Building civic impact']).map((badge) => <span key={badge} className="status-pill amber">★ {badge}</span>)}
        </div>
      </div>

      <div className="leaderboard-profile-actions">
        <button className="button primary" type="button" onClick={onFollow} disabled={followLoading || profile.isCurrentUser}>
          {profile.isCurrentUser ? 'This is your profile' : followLoading ? 'Updating…' : profile.isFollowing ? 'Unfollow citizen' : 'Follow citizen'}
        </button>
      </div>
      <p className="leaderboard-profile-privacy">Only civic contribution information is shown. Email, phone number and private account details are never displayed.</p>
    </div>
  );
}

function ProfileStat({ label, value }) {
  return <div><strong>{value}</strong><span>{label}</span></div>;
}

function Podium({ row, first, fallbackRank, onOpen }) {
  return (
    <button className={`podium-card leaderboard-podium-button ${first ? 'first' : ''}`} type="button" onClick={onOpen} disabled={!row}>
      <div className="podium-avatar">{row ? initials(row.citizenName) : fallbackRank}</div>
      <h3>{row?.citizenName || `Rank ${fallbackRank}`}</h3>
      <p>#{row?.rank || fallbackRank} · {row?.points ?? '—'} points</p>
    </button>
  );
}

function Notice({ title, text }) {
  return <div className="notification-item"><span>★</span><div><strong>{title}</strong><small>{text}</small></div></div>;
}

function initials(name) {
  return name?.split(' ').filter(Boolean).map((part) => part[0]).slice(0, 2).join('').toUpperCase() || 'CH';
}
