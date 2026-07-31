import { Component } from 'react';
import { Link } from 'react-router-dom';

export default class OfficerPageErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, errorMessage: '' };
  }

  static getDerivedStateFromError(error) {
    return {
      hasError: true,
      errorMessage: error?.message || 'The officer page could not be displayed.',
    };
  }

  componentDidCatch(error, info) {
    // Keep a useful browser-console record while showing the officer a recovery screen.
    console.error('Officer page rendering failed.', error, info);
  }

  componentDidUpdate(previousProps) {
    if (previousProps.resetKey !== this.props.resetKey && this.state.hasError) {
      this.setState({ hasError: false, errorMessage: '' });
    }
  }

  render() {
    if (!this.state.hasError) return this.props.children;

    return (
      <section className="page-wrap">
        <div className="surface">
          <div className="surface-header">
            <div>
              <p className="section-kicker">Officer workspace</p>
              <h2>We could not open this assignment</h2>
            </div>
          </div>
          <div className="surface-body">
            <div className="alert error" role="alert">
              {this.state.errorMessage}
            </div>
            <p className="muted">
              The page recovered instead of showing a blank screen. Return to the work queue and open the assignment again.
            </p>
            <div className="page-actions">
              <Link to="/officer/assignments" className="button primary">Open work queue</Link>
              <Link to="/officer" className="button outline">Back to dashboard</Link>
            </div>
          </div>
        </div>
      </section>
    );
  }
}
